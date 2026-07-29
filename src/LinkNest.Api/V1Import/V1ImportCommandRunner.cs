using LinkNest.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LinkNest.Api.V1Import;

/// <summary>
/// Command-line entry point for one-time v1 JSON imports.
/// </summary>
/// <remarks>
/// Usage:
/// <c>dotnet run --project src/LinkNest.Api -- --import-v1 --file path/to/export.json --user-id {guid}</c>
/// </remarks>
public static class V1ImportCommandRunner
{
    /// <summary>
    /// Runs the import command when present in <paramref name="args"/>; otherwise returns <see langword="null"/>.
    /// </summary>
    /// <returns>Process exit code when the command ran; <see langword="null"/> when args do not request import.</returns>
    public static async Task<int?> TryRunAsync(string[] args, WebApplication app)
    {
        if (!args.Contains("--import-v1", StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        var filePath = ReadOptionValue(args, "--file");
        var userIdValue = ReadOptionValue(args, "--user-id");

        if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(userIdValue))
        {
            app.Logger.LogError(
                "Usage: --import-v1 --file <path-to-export.json> --user-id <identity-user-guid>");
            return 2;
        }

        if (!File.Exists(filePath))
        {
            app.Logger.LogError("Import file not found: {FilePath}", filePath);
            return 2;
        }

        if (!Guid.TryParse(userIdValue, out var targetUserId))
        {
            app.Logger.LogError("Invalid --user-id value: {UserId}", userIdValue);
            return 2;
        }

        var json = await File.ReadAllTextAsync(filePath);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (dbContext.Database.IsRelational()
            && dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true)
        {
            await dbContext.Database.MigrateAsync();
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        var importer = scope.ServiceProvider.GetRequiredService<V1AppDataImportService>();
        var result = await importer.ImportFromJsonAsync(json, targetUserId);

        if (result.InvalidPayload)
        {
            app.Logger.LogError("Import failed: payload is not valid v1 JSON.");
            return 3;
        }

        if (result.UserNotFound)
        {
            app.Logger.LogError("Import failed: user {UserId} was not found.", targetUserId);
            return 4;
        }

        app.Logger.LogInformation(
            "Import complete. Categories imported={CategoriesImported}, skipped={CategoriesSkipped}; links imported={LinksImported}, skipped={LinksSkipped}.",
            result.CategoriesImported,
            result.CategoriesSkipped,
            result.LinksImported,
            result.LinksSkipped);

        return 0;
    }

    private static string? ReadOptionValue(string[] args, string optionName)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (args[index].Equals(optionName, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }
}

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;

namespace ManageFamilyMeals.Api.Identity;

public static class IdentityServiceExtensions
{
    public const string ApplicationCookieName = ".ManageFamilyMeals.Auth";

    public static IServiceCollection AddManageFamilyMealsIdentity(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<IdentitySeedOptions>(configuration.GetSection(IdentitySeedOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.AddScoped<IdentityDataSeeder>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddScoped<EmailConfirmationService>();

        var configuredPath = configuration["DataProtection:KeysPath"];
        EnsureDataProtectionKeysConfigured(configuredPath, environment);
        var dataProtectionPath = ResolveDataProtectionPath(configuredPath, environment);

        Directory.CreateDirectory(dataProtectionPath);
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
            .SetApplicationName("ManageFamilyMeals");

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.SignIn.RequireConfirmedEmail = configuration.GetValue<bool>($"{AuthOptions.SectionName}:RequireConfirmedEmail", true);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<Data.AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
            })
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.Cookie.Name = ApplicationCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";
                options.Cookie.SecurePolicy = environment.IsDevelopment() || environment.IsEnvironment("Testing")
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.Events.OnRedirectToLogin = context =>
                {
                    if (HttpMethods.IsGet(context.Request.Method)
                        && !context.Request.Path.StartsWithSegments("/api"))
                    {
                        var returnUrl = Uri.EscapeDataString(
                            context.Request.Path + context.Request.QueryString);
                        context.Response.Redirect($"/login?returnUrl={returnUrl}");
                        return Task.CompletedTask;
                    }

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static void EnsureDataProtectionKeysConfigured(string? configuredPath, IHostEnvironment environment)
    {
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException(
                "DataProtection:KeysPath must be configured in non-development environments so API and Web hosts share the same key ring.");
        }

        var expandedPath = Environment.ExpandEnvironmentVariables(configuredPath);
        if (string.IsNullOrWhiteSpace(expandedPath) || expandedPath.Contains('%'))
        {
            throw new InvalidOperationException(
                "DataProtection:KeysPath is set but its environment variables are unresolved. " +
                "Set MFM_DATA_PROTECTION_KEYS_PATH to a shared directory accessible by both API and Web hosts.");
        }
    }

    public static string ResolveDataProtectionPath(string? configuredPath, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ManageFamilyMeals",
                "DataProtection-Keys");
        }

        var expandedPath = Environment.ExpandEnvironmentVariables(configuredPath)
            .Replace('/', Path.DirectorySeparatorChar);

        return Path.IsPathRooted(expandedPath)
            ? expandedPath
            : Path.Combine(environment.ContentRootPath, expandedPath);
    }
}

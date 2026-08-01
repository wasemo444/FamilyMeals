namespace LinkNest.Shared.Configuration;

/// <summary>
/// Converts PostgreSQL URI connection strings (as provided by Neon/Supabase) into
/// Npgsql key/value format. Npgsql 10 does not accept libpq URIs directly.
/// </summary>
public static class ConnectionStringNormalizer
{
    public static string Normalize(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var (username, password) = ParseUserInfo(uri.UserInfo);
        var database = uri.AbsolutePath.TrimStart('/');

        var parts = new List<string>
        {
            $"Host={uri.Host}",
            $"Database={database}",
            $"Username={username}",
            $"Password={password}",
        };

        if (uri.Port is > 0 and not 5432)
        {
            parts.Insert(1, $"Port={uri.Port}");
        }

        var hasSslMode = false;
        foreach (var (key, value) in ParseQuery(uri.Query))
        {
            if (!key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            parts.Add($"SSL Mode={MapSslMode(value)}");
            hasSslMode = true;
        }

        if (!hasSslMode)
        {
            parts.Add("SSL Mode=Require");
        }

        return string.Join(';', parts);
    }

    private static (string Username, string Password) ParseUserInfo(string userInfo)
    {
        if (string.IsNullOrEmpty(userInfo))
        {
            return (string.Empty, string.Empty);
        }

        var colonIndex = userInfo.IndexOf(':');
        if (colonIndex < 0)
        {
            return (Uri.UnescapeDataString(userInfo), string.Empty);
        }

        return (
            Uri.UnescapeDataString(userInfo[..colonIndex]),
            Uri.UnescapeDataString(userInfo[(colonIndex + 1)..]));
    }

    private static IEnumerable<(string Key, string Value)> ParseQuery(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            yield break;
        }

        var trimmed = query.TrimStart('?');
        foreach (var segment in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var equalsIndex = segment.IndexOf('=');
            if (equalsIndex < 0)
            {
                yield return (Uri.UnescapeDataString(segment), string.Empty);
                continue;
            }

            yield return (
                Uri.UnescapeDataString(segment[..equalsIndex]),
                Uri.UnescapeDataString(segment[(equalsIndex + 1)..]));
        }
    }

    private static string MapSslMode(string value) =>
        value.ToLowerInvariant() switch
        {
            "disable" => "Disable",
            "allow" => "Allow",
            "prefer" => "Prefer",
            "require" => "Require",
            "verify-ca" => "VerifyCA",
            "verify-full" => "VerifyFull",
            _ => "Require",
        };
}

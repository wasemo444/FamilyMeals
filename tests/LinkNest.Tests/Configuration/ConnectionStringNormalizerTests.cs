using LinkNest.Shared.Configuration;
using Npgsql;

namespace LinkNest.Tests.Configuration;

public class ConnectionStringNormalizerTests
{
    [Fact]
    public void Normalize_leaves_key_value_connection_strings_unchanged()
    {
        const string input = "Host=localhost;Database=linknest;Username=user;Password=pass";

        var result = ConnectionStringNormalizer.Normalize(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void Normalize_converts_neon_uri_to_npgsql_format()
    {
        const string input =
            "postgresql://user:pass@ep-test.neon.tech/neondb?sslmode=require&channel_binding=require";

        var normalized = ConnectionStringNormalizer.Normalize(input);
        var builder = new NpgsqlConnectionStringBuilder(normalized);

        Assert.Equal("ep-test.neon.tech", builder.Host);
        Assert.Equal("neondb", builder.Database);
        Assert.Equal("user", builder.Username);
        Assert.Equal("pass", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
    }

    [Fact]
    public void Normalize_defaults_ssl_mode_for_uri_without_query()
    {
        const string input = "postgresql://user:pass@ep-test.neon.tech/neondb";

        var normalized = ConnectionStringNormalizer.Normalize(input);
        var builder = new NpgsqlConnectionStringBuilder(normalized);

        Assert.Equal(SslMode.Require, builder.SslMode);
    }
}

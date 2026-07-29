using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LinkNest.Api.Identity;

/// <summary>
/// Applies JWT secret fallback for development and testing environments after configuration binding.
/// </summary>
internal sealed class ConfigureJwtOptions(IHostEnvironment environment) : IConfigureOptions<JwtOptions>
{
    /// <inheritdoc />
    public void Configure(JwtOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Secret) && options.Secret.Length >= 32)
        {
            return;
        }

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            throw new InvalidOperationException(
                "Jwt:Secret must be configured with at least 32 characters in non-development environments.");
        }

        options.Secret = "LinkNest.Dev.Jwt.SigningKey.Minimum32Chars!";
    }
}

/// <summary>
/// Configures JWT bearer validation from bound <see cref="JwtOptions"/> at runtime.
/// </summary>
internal sealed class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions) : IPostConfigureOptions<JwtBearerOptions>
{
    /// <inheritdoc />
    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        var jwt = jwtOptions.Value;
        options.MapInboundClaims = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userManager = context.HttpContext.RequestServices
                    .GetRequiredService<UserManager<ApplicationUser>>();

                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    context.Fail("Invalid token subject.");
                    return;
                }

                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user is null || !user.IsActive)
                {
                    context.Fail("Account is not active.");
                }
            }
        };
    }
}

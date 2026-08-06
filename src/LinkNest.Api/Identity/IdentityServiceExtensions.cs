using LinkNest.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LinkNest.Api.Identity;

/// <summary>
/// Registers ASP.NET Identity, cookie authentication, and data protection for the API host.
/// </summary>
/// <remarks>
/// API cookie events return <c>401 Unauthorized</c> for unauthenticated API requests (not redirects)
/// and <c>403 Forbidden</c> for access-denied scenarios.
/// </remarks>
public static class IdentityServiceExtensions
{
    /// <summary>Application authentication cookie name shared with the web host.</summary>
    public const string ApplicationCookieName = ".LinkNest.Auth";

    /// <summary>Policy scheme that forwards to JWT or cookie auth based on the Authorization header.</summary>
    public const string SmartAuthScheme = "SmartAuth";

    /// <summary>
    /// Adds Identity, cookie auth, data protection, and related services to the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration (Auth, IdentitySeed, DataProtection sections).</param>
    /// <param name="environment">Host environment for cookie security and key path defaults.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when data protection keys are misconfigured outside development.</exception>
    /// <param name="enableOutboundEmail">
    /// When <see langword="false"/> (Web host), skips SMTP validation and uses log-only email.
    /// The API host sends confirmation/reset mail and should leave this <see langword="true"/>.
    /// </param>
    public static IServiceCollection AddLinkNestIdentity(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        bool enableOutboundEmail = true)
    {
        services.Configure<IdentitySeedOptions>(configuration.GetSection(IdentitySeedOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        var emailOptionsBuilder = services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName));

        if (enableOutboundEmail)
        {
            emailOptionsBuilder.ValidateOnStart();
            services.AddSingleton<IValidateOptions<EmailOptions>, EmailConfigurationValidator>();
        }

        services.AddSingleton<JwtTokenService>();
        services.AddScoped<IdentityDataSeeder>();

        if (enableOutboundEmail)
        {
            var useSmtp = configuration.GetValue<bool>($"{EmailOptions.SectionName}:UseSmtp")
                || (!environment.IsDevelopment() && !IsIntegrationTestEnvironment(environment));

            if (useSmtp)
            {
                services.AddSingleton<IEmailSender, SmtpEmailSender>();
                services.AddSingleton<SmtpConnectivityChecker>();
            }
            else
            {
                services.AddSingleton<IEmailSender, LoggingEmailSender>();
            }

            services.AddScoped<EmailConfirmationService>();
            services.AddScoped<PasswordResetService>();
        }
        else
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }

        ConfigureDataProtection(services, configuration, environment);

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
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IConfigureOptions<JwtOptions>, ConfigureJwtOptions>();
        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = SmartAuthScheme;
                options.DefaultChallengeScheme = SmartAuthScheme;
                options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
            })
            .AddPolicyScheme(SmartAuthScheme, SmartAuthScheme, policyOptions =>
            {
                policyOptions.ForwardDefaultSelector = context =>
                {
                    var authorization = context.Request.Headers.Authorization.ToString();
                    return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? JwtBearerDefaults.AuthenticationScheme
                        : IdentityConstants.ApplicationScheme;
                };
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, _ => { })
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.Cookie.Name = ApplicationCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";
                options.Cookie.SecurePolicy = environment.IsDevelopment() || IsIntegrationTestEnvironment(environment)
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

    private static bool IsIntegrationTestEnvironment(IHostEnvironment environment) =>
        environment.IsEnvironment("Testing") || environment.IsEnvironment("ProductionTesting");

    /// <summary>
    /// Returns whether Data Protection keys are stored in PostgreSQL instead of the filesystem.
    /// </summary>
    public static bool UsesDatabaseStorage(IConfiguration configuration) =>
        string.Equals(
            configuration["DataProtection:Storage"],
            DataProtectionStorageMode.Database,
            StringComparison.OrdinalIgnoreCase);

    private static void ConfigureDataProtection(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName("LinkNest");

        if (UsesDatabaseStorage(configuration))
        {
            dataProtection.PersistKeysToDbContext<AppDbContext>();
            return;
        }

        var configuredPath = configuration["DataProtection:KeysPath"];
        EnsureDataProtectionKeysConfigured(configuredPath, environment, useDatabase: false);
        var dataProtectionPath = ResolveDataProtectionPath(configuredPath, environment);

        Directory.CreateDirectory(dataProtectionPath);
        dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
    }

    /// <summary>
    /// Validates that a shared data protection key path is configured for non-development environments.
    /// </summary>
    /// <param name="configuredPath">Raw path from configuration (may contain environment variables).</param>
    /// <param name="environment">Host environment.</param>
    /// <param name="useDatabase">When <see langword="true"/>, filesystem path validation is skipped.</param>
    /// <exception cref="InvalidOperationException">Thrown when production/testing requires a key path but none is valid.</exception>
    public static void EnsureDataProtectionKeysConfigured(
        string? configuredPath,
        IHostEnvironment environment,
        bool useDatabase = false)
    {
        if (useDatabase || environment.IsDevelopment() || IsIntegrationTestEnvironment(environment))
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

    /// <summary>
    /// Resolves the filesystem directory used to persist data protection keys.
    /// </summary>
    /// <param name="configuredPath">Optional path from configuration; uses local app data when null in development.</param>
    /// <param name="environment">Host environment for content-root-relative paths.</param>
    /// <returns>Absolute directory path for the key ring.</returns>
    public static string ResolveDataProtectionPath(string? configuredPath, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LinkNest",
                "DataProtection-Keys");
        }

        var expandedPath = Environment.ExpandEnvironmentVariables(configuredPath)
            .Replace('/', Path.DirectorySeparatorChar);

        return Path.IsPathRooted(expandedPath)
            ? expandedPath
            : Path.Combine(environment.ContentRootPath, expandedPath);
    }
}

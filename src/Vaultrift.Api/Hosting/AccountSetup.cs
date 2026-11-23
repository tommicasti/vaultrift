using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Vaultrift.Infrastructure.Accounts;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Api.Hosting;

public static class AccountSetup
{
    public const string SignInPolicy = "sign-in";

    public static IServiceCollection AddAccounts(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var provider = configuration["Storage:Provider"] ?? "File";

        IAccountRepository repository = provider switch
        {
            "SqlServer" => new SqlAccountRepository(
                configuration.GetConnectionString(StorageSetup.ConnectionName)
                ?? throw new InvalidOperationException($"Storage:Provider is SqlServer but ConnectionStrings:{StorageSetup.ConnectionName} is missing.")),
            "File" => new FileAccountRepository(Path.Combine(environment.ContentRootPath, configuration["Storage:Folder"] ?? "saves")),
            _ => new MemoryAccountRepository()
        };

        services.AddSingleton(repository);
        services.AddSingleton(provider => new AccountService(provider.GetRequiredService<IAccountRepository>(), provider.GetRequiredService<ISaveRepository>()));
        services.AddSingleton(provider => new TrophyService(provider.GetRequiredService<IAccountRepository>(), provider.GetRequiredService<AccountService>()));

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "vaultrift.account";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
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

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(SignInPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });

        return services;
    }

    public static WebApplication UseAccounts(this WebApplication app)
    {
        app.UseRateLimiter();
        app.UseAuthentication();
        return app;
    }

    public static Guid? AccountId(this ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true && Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

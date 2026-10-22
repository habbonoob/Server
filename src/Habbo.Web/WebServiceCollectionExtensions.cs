using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Habbo.Web;

public static class WebServiceCollectionExtensions
{
    public static IServiceCollection AddWebSeam(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection("Auth"));
        var auth = configuration.GetSection("Auth").Get<AuthOptions>() ?? new AuthOptions();
        var protection = services.AddDataProtection().SetApplicationName("Habbo");
        if (auth.KeysDirectory.Length > 0)
            protection.PersistKeysToFileSystem(new DirectoryInfo(auth.KeysDirectory));
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(cookie =>
        {
            cookie.Cookie.Name = WebSession.CookieName;
            cookie.Cookie.HttpOnly = true;
            cookie.Cookie.SameSite = SameSiteMode.Lax;
            cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            cookie.ExpireTimeSpan = TimeSpan.FromDays(auth.SessionDays);
            cookie.SlidingExpiration = true;
            // an API: say so instead of redirecting to a login page
            cookie.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            cookie.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });
        services.AddAuthorization();
        services.AddHttpClient();
        services.AddHttpClient(AssetMirror.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(AssetMirror.UserAgent);
        });
        services.AddSingleton<AssetMirror>();
        services.AddSingleton<GroupBadgeImager>();
        services.AddHostedService<SsoTicketCleanup>();
        services.AddCors(options =>
            options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
        services.AddWebRateLimits();
        return services;
    }
}

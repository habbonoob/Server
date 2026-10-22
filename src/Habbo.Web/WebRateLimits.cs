using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Habbo.Web;

/// <summary>How often one address may use the endpoints anyone can reach without logging in: logging in (each try
/// costs a password hash, and guessing should be slow) posting crash reports (each is written to the log), drawing group badges and avatars (each may be drawn and fetched anew), the asset mirror (a file it lacks is fetched and kept), and asking for SSO tickets (each is a row).</summary>
public static class WebRateLimits
{
    public const string Login = "login";
    public const string Register = "register";
    public const string CrashReports = "crash-reports";
    public const string Badges = "badges";
    public const string Mirror = "mirror";
    public const string HotelTickets = "hotel-tickets";

    public static IServiceCollection AddWebRateLimits(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Login, context => PerAddress(context, permits: 10));
            options.AddPolicy(Register, context => PerAddress(context, permits: 5));
            options.AddPolicy(CrashReports, context => PerAddress(context, permits: 5));
            // a room full of group badges asks for each of them, so this one is generous; it only keeps one address from
            // drawing badge after badge (or avatar after avatar) to fill the cache and the CDN's queue
            options.AddPolicy(Badges, context => PerAddress(context, permits: 600));
            // a client loading the hotel asks for thousands of assets, so this one is more generous still; it only keeps
            // one address from asking for path after path to be fetched, kept and noted as wanted
            options.AddPolicy(Mirror, context => PerAddress(context, permits: 5000));
            // the website asks for one each time the hotel is entered
            options.AddPolicy(HotelTickets, context => PerAddress(context, permits: 10));
        });

    /// <summary>So many requests a minute from one address.</summary>
    private static RateLimitPartition<string> PerAddress(HttpContext context, int permits) =>
        RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
}

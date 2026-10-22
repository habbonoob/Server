using System.Security.Cryptography;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Habbo.Web;

/// <summary>Entering the hotel from the website: an SSO ticket for the logged-in account, which the client logs in
/// with. It is good for a few minutes and one login.</summary>
public static class HotelEndpoints
{
    public static void MapHotelEndpoints(this WebApplication app) =>
        app.MapPost("/api/hotel/ticket", TicketAsync).RequireRateLimiting(WebRateLimits.HotelTickets);

    private static async Task<IResult> TicketAsync(
        HttpContext context, IDbContextFactory<AppDbContext> dbFactory, IOptions<AuthOptions> options, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (await WebSession.UserAsync(context, db, ct) is not { } user)
            return Results.Unauthorized();

        var ticket = new SsoTicket
        {
            Ticket = RandomNumberGenerator.GetHexString(32),
            UserId = user.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(options.Value.TicketLifetimeMinutes),
        };
        db.SsoTickets.Add(ticket);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new HotelTicket(ticket.Ticket, ticket.ExpiresAt));
    }
}

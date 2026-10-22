using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace Habbo.Silo.Grains.Auth;

public sealed class AuthGrain(
    IDbContextFactory<AppDbContext> dbFactory,
    IOptions<SsoTicketOptions> ticketOptions,
    ILogger<AuthGrain> logger) : Grain, IAuthGrain
{
    public async Task<AuthResult> AuthenticateAsync()
    {
        var ticketValue = this.GetPrimaryKeyString();

        await using var db = await dbFactory.CreateDbContextAsync();
        var singleUse = ticketOptions.Value.SingleUse;
        var ticket = await db.SsoTickets
            .Where(t => t.Ticket == ticketValue)
            .Where(t => t.Permanent || ((!singleUse || t.ConsumedAt == null) && t.ExpiresAt > DateTimeOffset.UtcNow))
            .FirstOrDefaultAsync();

        if (ticket is null)
            return AuthResult.Failed("invalid_or_expired_ticket");

        // Stamped either way so the first use is still on record; only single-use mode rejects a second one.
        ticket.ConsumedAt ??= DateTimeOffset.UtcNow;

        var user = await db.Users.FindAsync(ticket.UserId);
        if (user is null)
        {
            logger.LogWarning("SSO ticket {Ticket} points at missing user {UserId}", ticketValue, ticket.UserId);
            await db.SaveChangesAsync();
            return AuthResult.Failed("unknown_user");
        }

        // Read before it is overwritten: what the account knew as its last login is this
        // login's previous one, and nothing else records it.
        var previousLoginAt = user.LastLoginAt;
        user.LastLoginAt = DateTimeOffset.UtcNow;
        // a day in a row more when the last login was yesterday; the same streak later the same day; else a new one
        var daysSince = previousLoginAt is { } previous ? (user.LastLoginAt.Value.UtcDateTime.Date - previous.UtcDateTime.Date).Days : -1;
        user.LoginStreakDays = daysSince switch
        {
            0 => Math.Max(1, user.LoginStreakDays),
            1 => user.LoginStreakDays + 1,
            _ => 1,
        };
        await db.SaveChangesAsync();

        logger.LogInformation(
            ticket.Permanent ? "Permanent SSO ticket accepted for user {Username}"
            : singleUse ? "SSO ticket consumed for user {Username}" : "SSO ticket accepted (reusable) for user {Username}",
            user.Username);
        // The player grain is the one projection of the account, so the login sees what every
        // later read will.
        return AuthResult.Ok(await GrainFactory.GetGrain<IPlayerGrain>(user.Id).ReloadAsync(), previousLoginAt);
    }
}

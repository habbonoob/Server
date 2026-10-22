using Habbo.Domain.Subscriptions;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Handshake;
using Habbo.Protocol.Messages.Outgoing.Users;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Players;

/// <summary>Habbo Club membership bought: it runs on from where it ends, or from now when it has run
/// out, and the player's club level is the best they bought.</summary>
public sealed partial class PlayerGrain
{
    private async Task ExtendMembershipAsync(AppDbContext db, User user, ClubLevel level, TimeSpan length, int months)
    {
        var now = DateTimeOffset.UtcNow;
        var membership = await db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == user.Id && s.ProductName == SubscriptionProducts.HabboClub);
        if (membership is null)
        {
            membership = new Subscription { UserId = user.Id, ProductName = SubscriptionProducts.HabboClub, StartedAt = now, ExpiresAt = now };
            db.Subscriptions.Add(membership);
        }

        // a lapsed membership starts a new run
        if (!membership.IsActive(now))
        {
            if (membership.StreakStartedAt is { } started)
                membership.PastVipDays += (int)(membership.ExpiresAt - started).TotalDays;
            membership.ExpiresAt = now;
            membership.StreakStartedAt = now;
        }

        membership.ExpiresAt += length;
        membership.MemberPeriods += months;
        membership.LastModifiedAt = now;
        if (level > user.ClubLevel)
            user.ClubLevel = level;
    }

    private Task<bool> HasActiveMembershipAsync(AppDbContext db) =>
        db.Subscriptions.AnyAsync(s => s.UserId == PlayerId && s.ProductName == SubscriptionProducts.HabboClub
                                       && s.ExpiresAt > DateTimeOffset.UtcNow);

    /// <summary>Ends the club level of an online player whose membership ran out since they logged in.</summary>
    private async Task ExpireLapsedMembershipAsync()
    {
        if (Data is not { ClubLevel: > ClubLevel.None })
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await LoadUserAsync(db);
        if (user.ClubLevel == ClubLevel.None)
            await AnnounceMembershipAsync(ClubLevel.None);
    }

    /// <summary>Tells the player their membership changed, which empties and reloads their catalog.</summary>
    private async Task AnnounceMembershipAsync(ClubLevel level)
    {
        Data = Data is null ? null : Data with { ClubLevel = level };
        if (Data is not null)
            Send(UserRightsMessageComposer.From(Data));
        Send(ScrSendUserInfoComposer.From(
            await subscriptions.GetAsync(PlayerId, SubscriptionProducts.HabboClub), (int)SubscriptionResponseType.Changed));
    }
}

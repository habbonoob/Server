using Habbo.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A badge display case: bought with one of the buyer's badges (the shop's badge selector), which it shows
/// engraved with the buyer's name and the date.</summary>
[FurniInteraction("furniture_badge_display")]
public sealed class BadgeDisplayInteraction : FurniInteraction
{
    private const int Shown = 0;

    public override async Task<bool> AcceptsOrderAsync(AppDbContext db, Guid buyerId, string clientParam) =>
        clientParam.Length > 0 && await db.UserBadges.AnyAsync(b => b.UserId == buyerId && b.Code == clientParam);

    /// <summary>One handed over any other way — a prize, a gift unwrapped — shows no badge.</summary>
    public override void Grant(FurniGrant grant) => FurniDataStore.Attach(grant.Db, grant.Item, grant.ClientParam.Length > 0
        ? BadgeEngraving.Of(Shown, grant.ClientParam, grant.BuyerName, grant.At)
        : BadgeEngraving.Blank(Shown));
}

using Habbo.Domain.Catalog;
using Habbo.Domain.Furniture;
using Habbo.Silo.Items.Interactions;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Players;

/// <summary>The bonus rare: so many credits got earn the player one (Catalog:BonusRareClassName, BonusRareCredits). On
/// Habbo they are credits bought; spending in the catalogue does not count (habbo.com, 25 October 2026: 15 credits spent
/// with 10 to go left it at 10). The hotel sells none, so the credits staff give and vouchers hold count instead. Credits
/// beyond it carry on towards the next (habbo.com, 25 October 2026: 55 credits bought with 10 to go gave a bag and left
/// 75 to go).</summary>
public sealed partial class PlayerGrain
{
    public async Task CountTowardsBonusRareAsync(int credits)
    {
        var bonus = catalogExtras.Value;
        if (credits <= 0 || bonus.BonusRareCredits <= 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var rare = await db.ItemDefinitions.FirstOrDefaultAsync(d => d.ClassName == bonus.BonusRareClassName);
        if (rare is null)
            return;

        var progress = await db.BonusRareProgress.FirstOrDefaultAsync(p => p.UserId == PlayerId);
        if (progress is null)
            db.BonusRareProgress.Add(progress = new BonusRareProgress { UserId = PlayerId });
        progress.CreditsSpent += credits;

        var earned = new List<UserItem>();
        while (progress.CreditsSpent >= bonus.BonusRareCredits)
        {
            progress.CreditsSpent -= bonus.BonusRareCredits;
            var item = new UserItem { OwnerId = PlayerId, DefinitionId = rare.Id };
            db.UserItems.Add(item);
            interactions.For(rare).Grant(new FurniGrant(db, item, rare, string.Empty, string.Empty, string.Empty, DateTime.UtcNow));
            earned.Add(item);
        }

        await db.SaveChangesAsync();
        await AnnounceItemsAsync(earned.Select(i => i.Id).ToList());

        // the hotel view's bonus bar moves on with the purchase
        Send(new Habbo.Protocol.Messages.Outgoing.Catalog.BonusRareInfoMessageComposer(rare.ClassName, rare.ClassId, bonus.BonusRareCredits,
            Math.Max(0, bonus.BonusRareCredits - progress.CreditsSpent)));
    }
}

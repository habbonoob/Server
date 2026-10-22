using Habbo.Domain.Catalog;
using Habbo.Domain.Furniture;
using Habbo.Silo.Items.Interactions;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Players;

/// <summary>The bonus rare: every so many credits spent in the catalog earn the player one
/// (Catalog:BonusRareClassName, BonusRareCredits).</summary>
public sealed partial class PlayerGrain
{
    private async Task CountTowardsBonusRareAsync(int credits)
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
    }
}

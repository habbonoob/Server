using System.Globalization;
using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Recycler;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Outgoing.Recycler;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Inventory;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Recycler;

/// <summary>The Furni-Matic: recyclable items in, a box out holding a prize drawn from its levels, the
/// rarest first, each by its odds; the commonest when no rarer one comes up. It waits between uses.</summary>
public sealed class RecyclerService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    PlayerMessenger messenger,
    IOptions<RecyclerOptions> options) : IRecyclerService
{
    private const int NoWait = 0;
    private const int NoPrize = 0;

    public async Task SendStatusAsync(PlayerData player)
    {
        if (!options.Value.Enabled)
        {
            await messenger.SendAsync(player.Id, new RecyclerStatusMessageComposer(RecyclerStatusMessageComposer.Closed, NoWait));
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var wait = await SecondsToWaitAsync(db, player.Id);
        await messenger.SendAsync(player.Id, new RecyclerStatusMessageComposer(
            wait > 0 ? RecyclerStatusMessageComposer.CoolingDown : RecyclerStatusMessageComposer.Ready, wait));
    }

    private async Task<int> SecondsToWaitAsync(AppDbContext db, Guid playerId)
    {
        var last = await db.RecyclerUses.Where(u => u.UserId == playerId).Select(u => (DateTimeOffset?)u.LastRecycledAt).FirstOrDefaultAsync();
        return last is null ? 0 : (int)Math.Max(0, Math.Ceiling((last.Value.AddSeconds(options.Value.TimeoutSeconds) - DateTimeOffset.UtcNow).TotalSeconds));
    }

    public async Task SendPrizesAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var levels = await db.RecyclerPrizeLevels.AsNoTracking().Include(l => l.Prizes).ThenInclude(p => p.Definition)
            .OrderBy(l => l.Id).ToListAsync();
        await messenger.SendAsync(player.Id, new RecyclerPrizesMessageComposer(levels.Select(level => new RecyclerPrizeLevelData(
            level.Id, level.ProbabilityDenominator,
            level.Prizes.Where(p => p.Definition is not null).Select(p => new RecyclerPrizeData(p.Definition!.ClassName, 1,
                p.Definition.Type == ItemType.Wall ? Habbo.Protocol.Messages.Outgoing.Catalog.CatalogProductData.WallType : Habbo.Protocol.Messages.Outgoing.Catalog.CatalogProductData.FloorType,
                p.Definition.ClassId)).ToList())).ToList()));
    }

    public async Task RecycleAsync(PlayerData player, IReadOnlyList<int> itemIds)
    {
        var rules = options.Value;
        await using var db = await dbFactory.CreateDbContextAsync();
        var ids = itemIds.Distinct().ToList();
        var items = await db.UserItems.Include(i => i.Definition)
            .Where(i => ids.Contains(i.Id) && i.OwnerId == player.Id && i.RoomId == null && !i.InMarketplace && i.ChestId == null && !db.UserItems.Any(stored => stored.ChestId == i.Id) && !db.ChestCoins.Any(c => c.ItemId == i.Id && c.Coins > 0) && i.Recyclable && i.Definition!.Recyclable)
            .ToListAsync();
        var box = await db.ItemDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.ClassName == rules.BoxClassName);
        var prize = await DrawAsync(db);
        if (!rules.Enabled || ids.Count != rules.ItemsRequired || items.Count != ids.Count || box is null || prize is null
            || await SecondsToWaitAsync(db, player.Id) > 0)
        {
            await messenger.SendAsync(player.Id, new RecyclerFinishedMessageComposer(RecyclerFinishedMessageComposer.Failed, NoPrize));
            return;
        }

        // the items go only if they are all still the player's to recycle: a trade or an offer may have taken one since
        await using var transaction = await db.Database.BeginTransactionAsync();
        var gone = await db.UserItems.Where(i => ids.Contains(i.Id) && i.OwnerId == player.Id && i.RoomId == null && !i.InMarketplace
                                                 && i.ChestId == null && !db.UserItems.Any(stored => stored.ChestId == i.Id) && !db.ChestCoins.Any(c => c.ItemId == i.Id && c.Coins > 0))
            .ExecuteDeleteAsync();
        if (gone != items.Count)
        {
            await transaction.RollbackAsync();
            await messenger.SendAsync(player.Id, new RecyclerFinishedMessageComposer(RecyclerFinishedMessageComposer.Failed, NoPrize));
            return;
        }

        var boxItem = new UserItem { OwnerId = player.Id, DefinitionId = box.Id };
        db.UserItems.Add(boxItem);
        FurniDataStore.Attach(db, boxItem, FurniDataView.WithState(DateTime.UtcNow.ToString(rules.BoxDateFormat, CultureInfo.InvariantCulture)));
        db.FurniGifts.Add(new FurniGift { Item = boxItem, SenderId = player.Id, PrizeDefinitionId = prize.DefinitionId });
        var use = await db.RecyclerUses.FirstOrDefaultAsync(u => u.UserId == player.Id);
        if (use is null)
            db.RecyclerUses.Add(use = new RecyclerUse { UserId = player.Id });
        use.LastRecycledAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        foreach (var item in items)
            await messenger.SendAsync(player.Id, FurniListRemoveComposer.Item(item.Id));
        await grains.GetGrain<IPlayerGrain>(player.Id).ProgressAchievementAsync(AchievementNames.RecycledItems, items.Count);
        await grains.GetGrain<IPlayerGrain>(player.Id).AnnounceItemsAsync([boxItem.Id]);
        await messenger.SendAsync(player.Id, new RecyclerFinishedMessageComposer(RecyclerFinishedMessageComposer.Done, boxItem.Id));
    }

    /// <summary>A prize: the rarest level whose odds come up, else the commonest; any of its prizes.</summary>
    private static async Task<RecyclerPrize?> DrawAsync(AppDbContext db)
    {
        var levels = await db.RecyclerPrizeLevels.AsNoTracking().Include(l => l.Prizes)
            .Where(l => l.Prizes.Any()).OrderByDescending(l => l.ProbabilityDenominator).ToListAsync();
        if (levels.Count == 0)
            return null;

        var level = levels.FirstOrDefault(l => l.ProbabilityDenominator <= 1 || Random.Shared.Next(l.ProbabilityDenominator) == 0) ?? levels[^1];
        return level.Prizes[Random.Shared.Next(level.Prizes.Count)];
    }
}

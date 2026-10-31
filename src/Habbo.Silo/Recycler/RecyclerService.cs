using System.Globalization;
using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Recycler;
using Habbo.Domain.Users;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Recycler;

/// <summary>The Furni-Matic: recyclable items in, a box out holding a prize drawn from its levels, the
/// rarest first, each by its odds; the commonest when no rarer one comes up. It waits between uses.</summary>
public sealed class RecyclerService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    PlayerMessenger messenger,
    IOptions<RecyclerOptions> options,
    ILogger<RecyclerService> logger) : IRecyclerService
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
        // rarest first, as habbo.com lists them
        var levels = await db.RecyclerPrizeLevels.AsNoTracking().Include(l => l.Prizes).ThenInclude(p => p.Definition)
            .OrderByDescending(l => l.Id).ToListAsync();
        await messenger.SendAsync(player.Id, new RecyclerPrizesMessageComposer(levels.Select(level => new RecyclerPrizeLevelData(
            level.Id, level.ProbabilityDenominator,
            level.Prizes.Select(PrizeData).OfType<RecyclerPrizeData>().ToList())).ToList()));
    }

    /// <summary>A prize as the client lists it: a furni by its code, type and class, a chat style as "chat_bubble_<id>"
    /// of type "chat_style" (habbo.com's own); null for furni the hotel no longer has.</summary>
    private static RecyclerPrizeData? PrizeData(RecyclerPrize prize) => prize switch
    {
        { ChatStyleId: { } style } => new RecyclerPrizeData(ChatStylePrefix + style, 1, ChatStyleType, style),
        { Definition: { } definition } => new RecyclerPrizeData(definition.ClassName, 1,
            definition.Type == ItemType.Wall ? Habbo.Protocol.Messages.Outgoing.Catalog.CatalogProductData.WallType : Habbo.Protocol.Messages.Outgoing.Catalog.CatalogProductData.FloorType,
            definition.ClassId),
        _ => null,
    };

    private const string ChatStylePrefix = "chat_bubble_";
    private const string ChatStyleType = "chat_style";

    /// <summary>The currency the Furni-Matic is paid in.</summary>
    private const int Duckets = (int)Habbo.Domain.Catalog.ActivityPointType.Ducket;

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
        var refusal = !rules.Enabled ? "closed"
            : ids.Count != rules.ItemsRequired ? $"{ids.Count} items, not {rules.ItemsRequired}"
            : items.Count != ids.Count ? $"not recyclable here: {string.Join(", ", ids.Except(items.Select(i => i.Id)))}"
            : prize is null ? "no prizes"
            : prize.DefinitionId is not null && box is null ? $"no {rules.BoxClassName}"
            : await SecondsToWaitAsync(db, player.Id) > 0 ? "waiting"
            : null;
        if (refusal is not null || prize is null)
        {
            logger.LogInformation("Recycling refused for {Player}: {Reason}", player.Username, refusal);
            await messenger.SendAsync(player.Id, new RecyclerFinishedMessageComposer(RecyclerFinishedMessageComposer.Failed, NoPrize));
            return;
        }

        // the duckets first: one who cannot pay keeps their furni
        var playerGrain = grains.GetGrain<IPlayerGrain>(player.Id);
        if (rules.DucketCost > 0 && !await playerGrain.SpendActivityPointsAsync(Duckets, rules.DucketCost, CurrencyTransactionReasons.Recycler))
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
            if (rules.DucketCost > 0)
                await playerGrain.AddActivityPointsAsync(Duckets, rules.DucketCost, CurrencyTransactionReasons.Recycler);
            await messenger.SendAsync(player.Id, new RecyclerFinishedMessageComposer(RecyclerFinishedMessageComposer.Failed, NoPrize));
            return;
        }

        // furni comes in a box; a chat style is the player's at once
        UserItem? boxItem = null;
        if (prize.DefinitionId is { } prizeDefinitionId)
        {
            boxItem = new UserItem { OwnerId = player.Id, DefinitionId = box!.Id };
            db.UserItems.Add(boxItem);
            FurniDataStore.Attach(db, boxItem, FurniDataView.WithState(DateTime.UtcNow.ToString(rules.BoxDateFormat, CultureInfo.InvariantCulture)));
            db.FurniGifts.Add(new FurniGift { Item = boxItem, SenderId = player.Id, PrizeDefinitionId = prizeDefinitionId });
        }

        var use = await db.RecyclerUses.FirstOrDefaultAsync(u => u.UserId == player.Id);
        if (use is null)
            db.RecyclerUses.Add(use = new RecyclerUse { UserId = player.Id });
        use.LastRecycledAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        foreach (var item in items)
            await messenger.SendAsync(player.Id, FurniListRemoveComposer.Item(item.Id));
        await playerGrain.ProgressAchievementAsync(AchievementNames.RecycledItems, items.Count);
        if (boxItem is not null)
            await playerGrain.AnnounceItemsAsync([boxItem.Id]);
        if (prize.ChatStyleId is { } style)
            await playerGrain.ReceiveChatStyleAsync(style);
        await messenger.SendAsync(player.Id, new RecyclerFinishedMessageComposer(RecyclerFinishedMessageComposer.Done, boxItem?.Id ?? NoPrize));
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

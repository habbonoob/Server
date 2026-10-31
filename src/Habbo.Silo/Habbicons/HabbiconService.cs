using System.Collections.Concurrent;
using Habbo.Domain.Habbicons;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Protocol.Messages.Outgoing.Habbicons;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Silo.Abstractions.Habbicons;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Orleans;

namespace Habbo.Silo.Habbicons;

/// <summary>The Habbicons Collection (seed/habbicons.json) and the Habbicons each player owns: buying them, a set at a
/// time or one by one, claiming a complete set's reward, starring them, and using them.</summary>
public sealed class HabbiconService(
    IDbContextFactory<AppDbContext> dbFactory, IGrainFactory grains, PlayerMessenger messenger, HabbiconCatalog catalog)
    : IHabbiconService
{
    /// <summary>Why the money moved (CurrencyTransaction.Reason).</summary>
    private const string PurchaseReason = "habbicon_purchase";

    /// <summary>What the PurchaseOK for a Habbicon names it by: its title's key, habbicon_&lt;name&gt;_name.</summary>
    private const string LocalizationPrefix = "habbicon_";

    /// <summary>Ours: how soon after one Habbicon the next is shown, so the room is not flooded with them.</summary>
    private static readonly TimeSpan UseInterval = TimeSpan.FromSeconds(1);

    /// <summary>How many users are remembered before those whose wait is over are forgotten.</summary>
    private const int SweepAboveUsers = 1000;

    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _lastUsed = new();

    public async Task SendLoginAsync(PlayerData player)
    {
        var collections = await catalog.AllAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var owned = await OwnedAsync(db, player.Id);
        var recent = await db.UserHabbicons.AsNoTracking()
            .Where(h => h.UserId == player.Id && h.LastUsedAt != null)
            .OrderByDescending(h => h.LastUsedAt)
            .Take(HabbiconRules.RecentLimit)
            .Select(h => h.HabbiconId)
            .ToListAsync();

        await messenger.SendAsync(player.Id, new UserHabbiconsComposer(HabbiconRules.OwnedData(collections, owned), recent));
        await SendShopDataAsync(player.Id, collections, owned);
    }

    public async Task SendShopDataAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await SendShopDataAsync(player.Id, await catalog.AllAsync(), await OwnedAsync(db, player.Id));
    }

    public async Task SendInfoAsync(PlayerData player, int habbiconId)
    {
        if (await catalog.FindAsync(habbiconId) is not ({ } collection, { } habbicon))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        await messenger.SendAsync(player.Id,
            new HabbiconInfoComposer(HabbiconRules.DataOf(collection, habbicon, await OwnedAsync(db, player.Id))));
    }

    public async Task BuyAsync(PlayerData player, int habbiconId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var owned = await OwnedAsync(db, player.Id);
        if (await catalog.FindAsync(habbiconId) is not ({ } collection, { } habbicon) || !HabbiconRules.CanBuy(collection, habbicon, owned))
        {
            await messenger.SendAsync(player.Id, PurchaseErrorMessageComposer.Failed());
            return;
        }

        await PurchaseAsync(player, [habbicon], habbicon.Id, habbicon.Name,
            habbicon.PriceCredits, habbicon.PriceActivityPoints, habbicon.ActivityPointType);
    }

    public async Task BuyCollectionAsync(PlayerData player, int collectionId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var collection = await catalog.FindCollectionAsync(collectionId);
        var buying = collection is null ? [] : HabbiconRules.SetPurchase(collection, await OwnedAsync(db, player.Id));
        if (collection is null || buying.Count == 0)
        {
            await messenger.SendAsync(player.Id, PurchaseErrorMessageComposer.Failed());
            return;
        }

        await PurchaseAsync(player, buying, collection.Id, collection.Name,
            collection.PriceCredits, collection.PriceActivityPoints, collection.ActivityPointType);
    }

    public async Task ClaimAsync(PlayerData player, int habbiconId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await catalog.FindAsync(habbiconId) is not ({ } collection, { } reward)
            || !HabbiconRules.CanClaim(collection, habbiconId, await OwnedAsync(db, player.Id))
            || !await AddAsync(player.Id, [reward]))
        {
            await messenger.SendAsync(player.Id, PurchaseErrorMessageComposer.Failed());
            return;
        }

        // the hub waits for a purchase to go through before it asks for the collections again
        await messenger.SendAsync(player.Id, new PurchaseOKMessageComposer(OfferOf(reward.Id, reward.Name, 0, 0, 0), []));
        await GainedAsync(player.Id, [reward]);
    }

    public async Task SetFavouriteAsync(PlayerData player, int habbiconId, bool favourite)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.UserHabbicons.FirstOrDefaultAsync(h => h.UserId == player.Id && h.HabbiconId == habbiconId) is not { } owned
            || owned.Favourite == favourite)
            return;

        owned.Favourite = favourite;
        await db.SaveChangesAsync();
        await messenger.SendAsync(player.Id, new UserHabbiconStatusChangedComposer(habbiconId, HabbiconRules.OwnedState(favourite)));
    }

    public async Task<bool> UseAsync(Guid playerId, int habbiconId)
    {
        var now = DateTimeOffset.UtcNow;
        if (_lastUsed.TryGetValue(playerId, out var last) && now - last < UseInterval)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.UserHabbicons.Where(h => h.UserId == playerId && h.HabbiconId == habbiconId)
                .ExecuteUpdateAsync(s => s.SetProperty(h => h.LastUsedAt, now)) == 0)
            return false;

        _lastUsed[playerId] = now;
        if (_lastUsed.Count > SweepAboveUsers)
        {
            foreach (var over in _lastUsed.Where(u => now - u.Value >= UseInterval).Select(u => u.Key).ToList())
                _lastUsed.TryRemove(over, out _);
        }

        await grains.GetGrain<IPlayerGrain>(playerId).ProgressAchievementAsync(AchievementNames.HabbiconUsed, 1);
        return true;
    }

    public async Task<string?> OwnedNameAsync(Guid playerId, int habbiconId)
    {
        if (await catalog.FindAsync(habbiconId) is not (_, { } habbicon))
            return null;

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.UserHabbicons.AnyAsync(h => h.UserId == playerId && h.HabbiconId == habbiconId) ? habbicon.Name : null;
    }

    public async Task<bool> GrantAsync(Guid playerId, int habbiconId)
    {
        if (await catalog.FindAsync(habbiconId) is not (_, { } habbicon) || !await AddAsync(playerId, [habbicon]))
            return false;

        await GainedAsync(playerId, [habbicon]);
        return true;
    }

    private async Task SendShopDataAsync(Guid playerId, IReadOnlyList<HabbiconCollection> collections, IReadOnlyDictionary<int, bool> owned) =>
        await messenger.SendAsync(playerId, new HabbiconShopDataComposer(collections.Select(c => HabbiconRules.DataOf(c, owned)).ToList()));

    /// <summary>Takes the price, credits and activity points together, then hands the Habbicons over; a price that cannot
    /// be paid, or Habbicons already handed over meanwhile, leave the player as they were.</summary>
    private async Task PurchaseAsync(PlayerData player, IReadOnlyList<HabbiconEntry> habbicons, int offerId, string name,
        int credits, int activityPoints, int activityPointType)
    {
        var playerGrain = grains.GetGrain<IPlayerGrain>(player.Id);
        if (!await ChargeAsync(playerGrain, player.Id, credits, activityPoints, activityPointType))
        {
            await messenger.SendAsync(player.Id, PurchaseErrorMessageComposer.Failed());
            return;
        }

        if (!await AddAsync(player.Id, habbicons))
        {
            await RefundAsync(playerGrain, player.Id, credits, activityPoints, activityPointType);
            await messenger.SendAsync(player.Id, PurchaseErrorMessageComposer.Failed());
            return;
        }

        await messenger.SendAsync(player.Id, new PurchaseOKMessageComposer(OfferOf(offerId, name, credits, activityPoints, activityPointType), []));
        await GainedAsync(player.Id, habbicons);
    }

    private async Task<bool> ChargeAsync(IPlayerGrain playerGrain, Guid playerId, int credits, int activityPoints, int activityPointType)
    {
        if (credits > 0 && await playerGrain.SpendCreditsAsync(credits, PurchaseReason) is null)
            return false;

        if (activityPoints > 0 && !await playerGrain.SpendActivityPointsAsync(activityPointType, activityPoints, PurchaseReason))
        {
            await RefundAsync(playerGrain, playerId, credits, 0, activityPointType);
            return false;
        }

        return true;
    }

    private async Task RefundAsync(IPlayerGrain playerGrain, Guid playerId, int credits, int activityPoints, int activityPointType)
    {
        if (credits > 0)
            await messenger.SendAsync(playerId, new CreditBalanceComposer(await playerGrain.AddCreditsAsync(credits, PurchaseReason)));
        if (activityPoints > 0)
            await playerGrain.AddActivityPointsAsync(activityPointType, activityPoints, PurchaseReason);
    }

    /// <summary>Hands the Habbicons over, all or none: false when the player already owns any of them.</summary>
    private async Task<bool> AddAsync(Guid playerId, IReadOnlyList<HabbiconEntry> habbicons)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.UserHabbicons.AddRange(habbicons.Select(h => new UserHabbicon { UserId = playerId, HabbiconId = h.Id }));
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    /// <summary>Tells the player of Habbicons just theirs (their client says so, and marks them new), and counts them
    /// towards the achievements.</summary>
    private async Task GainedAsync(Guid playerId, IReadOnlyList<HabbiconEntry> habbicons)
    {
        foreach (var habbicon in habbicons)
            await messenger.SendAsync(playerId, new UserHabbiconStatusChangedComposer(habbicon.Id, HabbiconState.Owned));

        var collections = await catalog.AllAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var owned = await OwnedAsync(db, playerId);
        var playerGrain = grains.GetGrain<IPlayerGrain>(playerId);
        await playerGrain.ReachAchievementAsync(AchievementNames.HabbiconCollected, owned.Count);
        await playerGrain.ReachAchievementAsync(AchievementNames.HabbiconCollectionComp,
            collections.Count(c => c.Reward is { } reward && owned.ContainsKey(reward.Id)));
    }

    /// <summary>The offer a PurchaseOK echoes for a Habbicon or a set: nothing but its name and price, and no furniture.</summary>
    private static PurchaseOfferData OfferOf(int id, string name, int credits, int activityPoints, int activityPointType) =>
        new(id, LocalizationPrefix + name, IsRent: false, credits, activityPoints, activityPointType, Giftable: false,
            Products: [], ClubLevel: 0, BundlePurchaseAllowed: false);

    private static Task<Dictionary<int, bool>> OwnedAsync(AppDbContext db, Guid playerId) =>
        db.UserHabbicons.AsNoTracking().Where(h => h.UserId == playerId).ToDictionaryAsync(h => h.HabbiconId, h => h.Favourite);
}

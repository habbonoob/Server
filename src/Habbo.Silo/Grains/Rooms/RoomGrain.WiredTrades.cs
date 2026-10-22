using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Chests;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Transactions;
using Habbo.Silo.Chests;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Trading with wired, which is how things go into a chest: the player puts items into the trade window, a
/// furni chest taking any furniture but credit furni and a credit chest only credit furni, then accepts and confirms.
/// The furniture goes into the chest and becomes its owner's; credit furni becomes credits in it. Nothing may take
/// the chest past its capacity.</summary>
public sealed partial class RoomGrain
{
    private readonly Dictionary<Guid, WiredTrade> _wiredTrades = [];

    public Task StartChestDepositAsync(Guid playerId, int chestId)
    {
        if (_entities?.Find(playerId) is not { } user)
            return Task.CompletedTask;
        if (Chest(chestId) is not { } chest || !MayDepositIntoChest(user, chest))
        {
            SendTo(playerId, new WiredTransactionFailMessageComposer(WiredTransactionFailure.NoOrLockedChests));
            return Task.CompletedTask;
        }

        if (_trades.ContainsKey(playerId))
        {
            SendTo(playerId, new WiredTransactionFailMessageComposer(WiredTransactionFailure.AlreadyTrading));
            return Task.CompletedTask;
        }

        var replacing = _wiredTrades.Remove(playerId);
        var requirement = IsCoinChest(chest) ? TradeRequirementData.DonateCredits : TradeRequirementData.DonateFurni;
        _wiredTrades[playerId] = new WiredTrade(playerId, chestId, requirement);
        SendTo(playerId, new WiredTradeInitiateMessageComposer(
            new TradeRequirementData(requirement, string.Empty, string.Empty, null), ShowRequirementsImmediate: false, OverridePreviousTrade: replacing,
            TimeoutSeconds: 0));
        return Task.CompletedTask;
    }

    public async Task ChangeWiredTradeItemsAsync(Guid playerId, bool remove, IReadOnlyList<int> itemIds)
    {
        if (!_wiredTrades.TryGetValue(playerId, out var trade) || _entities?.Find(playerId) is not { } user)
            return;

        trade.Accepted = false;
        if (remove)
        {
            foreach (var itemId in itemIds)
            {
                trade.Items.Remove(itemId);
                trade.CreditValues.Remove(itemId);
                trade.Kinds.Remove(itemId);
            }

            if (trade.Terms is not null)
            {
                await using var removing = await dbFactory.CreateDbContextAsync();
                await RemeasureContractOfferAsync(removing, user, trade);
            }
            else
                SendWiredTradeItems(user, trade);
            return;
        }

        if (trade.Terms is not null)
        {
            await ChangeContractOfferAsync(user, trade, itemIds);
            return;
        }

        if (Chest(trade.ChestId) is not { } chest)
        {
            EndWiredTrade(trade, WiredTransactionFailure.ChestNotInRoom);
            return;
        }

        var wanted = itemIds.Distinct().Where(id => !trade.Items.ContainsKey(id)).ToList();
        await using var db = await dbFactory.CreateDbContextAsync();
        var items = await AvailableForWiredTradeAsync(db, playerId, wanted);

        // credit furni goes into a credit chest and nothing else does; chests stay out of chests
        var coins = IsCoinChest(chest);
        var fitting = items.Where(i => IsCreditFurni(i.Definition!) == coins && interactions.For(i.Definition!) is not ChestInteraction).ToList();
        if (fitting.Count < wanted.Count)
            SendTo(playerId, new WiredTradeTransactionNotificationMessageComposer(WiredTradeTransactionNotificationMessageComposer.InvalidItem));
        if (fitting.Count == 0)
            return;

        var capacity = await ChestCapacityAsync(chest);
        var held = await HeldByAsync(db, chest);
        var adding = coins ? fitting.Sum(i => i.Definition!.CreditValue ?? 0) : fitting.Count;
        var offered = coins ? trade.Credits : trade.Items.Count;
        if (held + offered + adding > capacity)
        {
            SendTo(playerId, new WiredTradeTransactionNotificationMessageComposer(WiredTradeTransactionNotificationMessageComposer.ExceedsChestCapacity));
            return;
        }

        var data = await furniData.LoadAsync(db, fitting);
        foreach (var item in fitting)
        {
            trade.Items[item.Id] = ToTradeItem(item, data[item.Id]);
            if (IsCreditFurni(item.Definition!))
                trade.CreditValues[item.Id] = item.Definition!.CreditValue ?? 0;
        }

        SendWiredTradeItems(user, trade);
    }

    public async Task ConfirmWiredTradeAsync(Guid playerId, bool confirmed)
    {
        if (!_wiredTrades.TryGetValue(playerId, out var trade) || _entities?.Find(playerId) is not { } user || trade.Items.Count == 0)
            return;

        // the first press accepts, once the offer is what is asked; the second, after the client's countdown, confirms
        if (!confirmed)
        {
            trade.Accepted = trade.Terms is null || OfferOf(trade.CreditValues, trade.Kinds).Measure(trade.Terms).Acceptable;
            return;
        }

        if (!trade.Accepted)
            return;

        if (trade.Terms is not null)
        {
            await CompleteContractTradeAsync(user, trade);
            return;
        }

        _wiredTrades.Remove(playerId);
        await CompleteChestDepositAsync(user, trade);
    }

    public async Task CancelWiredTradeAsync(Guid playerId)
    {
        if (!_wiredTrades.TryGetValue(playerId, out var trade))
            return;

        if (trade.Terms is not null)
            await EndContractTradeAsync(trade, WiredTransactionFailure.UserCancelled);
        else
            EndWiredTrade(trade, WiredTransactionFailure.UserCancelled);
    }

    private void EndWiredTrade(WiredTrade trade, int failure)
    {
        _wiredTrades.Remove(trade.PlayerId);
        SendTo(trade.PlayerId, new WiredTradeCancelledMessageComposer(failure));
    }

    private async Task CompleteChestDepositAsync(RoomEntityStore.RoomEntity user, WiredTrade trade)
    {
        if (Chest(trade.ChestId) is not { } chest || !MayDepositIntoChest(user, chest))
        {
            SendTo(user.Player.Id, new WiredTradeCancelledMessageComposer(WiredTransactionFailure.NoOrLockedChests));
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var items = await db.UserItems.Include(i => i.Definition)
            .Where(i => trade.Items.Keys.Contains(i.Id) && i.OwnerId == user.Player.Id && i.RoomId == null && i.ChestId == null && !i.InMarketplace)
            .ToListAsync();
        if (items.Count != trade.Items.Count)
        {
            SendTo(user.Player.Id, new WiredTradeCancelledMessageComposer(WiredTransactionFailure.FundsNoLongerAvailable));
            return;
        }

        var coins = IsCoinChest(chest);
        var adding = coins ? items.Sum(i => i.Definition!.CreditValue ?? 0) : items.Count;
        if (await HeldByAsync(db, chest) + adding > await ChestCapacityAsync(chest))
        {
            SendTo(user.Player.Id, new WiredTradeCancelledMessageComposer(WiredTransactionFailure.ExceedsChestCapacity));
            return;
        }

        var chestId = chest.Item.Id;
        var ids = items.Select(i => i.Id).ToList();
        await using var dbTransaction = await db.Database.BeginTransactionAsync();
        // the items are taken with one conditional write, as a trade takes them: read as free above, one may have been
        // listed, placed or traded from elsewhere since, and only the write finds out
        var free = db.UserItems.Where(i => ids.Contains(i.Id) && i.OwnerId == user.Player.Id && i.RoomId == null && i.ChestId == null && !i.InMarketplace);
        var taken = coins
            ? await free.ExecuteDeleteAsync()
            : await free.ExecuteUpdateAsync(s => s.SetProperty(i => i.ChestId, chestId).SetProperty(i => i.OwnerId, chest.Item.OwnerId));
        if (taken != ids.Count)
        {
            await dbTransaction.RollbackAsync();
            SendTo(user.Player.Id, new WiredTradeCancelledMessageComposer(WiredTransactionFailure.FundsNoLongerAvailable));
            return;
        }

        var transaction = NewChestTransaction(ChestTransaction.Manual, string.Empty, user.Player.Id, [chestId]);
        db.ChestTransactions.Add(transaction);
        if (coins)
        {
            // credit furni turns into the credits it is worth (it was deleted as it was taken)
            transaction.DepositedCoins = adding;
            var row = await db.ChestCoins.FirstOrDefaultAsync(c => c.ItemId == chestId);
            if (row is null)
                db.ChestCoins.Add(row = new ChestCoins { ItemId = chestId });
            row.Coins += adding;
            await db.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            HoldOf(chestId).Coins += adding;
            BroadcastToChestViewers(chestId, new CoinsChestContentsMessageComposer(chestId, row.Coins, true));
        }
        else
        {
            transaction.DepositedFurni = items.Count;
            await AddKindsAsync(db, transaction, items, deposited: true);
            await db.SaveChangesAsync();

            var now = DateTimeOffset.UtcNow;
            foreach (var item in items)
            {
                item.ChestId = chestId;
                item.OwnerId = chest.Item.OwnerId;
                item.StoredAt = now;
                item.StoredInTransactionId = transaction.Id;
                item.ChestDrawOrder = _chestRandom.Next();
            }

            await db.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            var data = await furniData.LoadAsync(db, items);
            HoldOf(chestId).Items.AddRange(items.Select(i => Held(i, data[i.Id])));
            BroadcastToChestViewers(chestId, new ItemsChestContentsUpdatedMessageComposer(chestId, [], items.Select(i => ToStorage(i, data[i.Id])).ToList()));
        }

        await PruneChestLogsAsync(db);
        SendTo(user.Player.Id, FurniListRemoveMultipleComposer.Items(items.Select(i => i.Id)));
        SendTo(user.Player.Id, new WiredTradeCompletedMessageComposer());
        SendTo(user.Player.Id, WiredTransactionSuccessMessageComposer.Of(WiredTransactionSuccessMessageComposer.Deposited));
        await RefreshChestLookAsync(chest);

        var settings = ChestSettings.Read(chest.Data);
        if (settings.NotifyDonation && user.Player.Id != chest.Item.OwnerId)
            await NotifyChestOwnerAsync(chest, DonationNotification, user.Player.Username);
        if (settings.NotifyChestFull && await HeldByAsync(db, chest) >= await ChestCapacityAsync(chest))
            await NotifyChestOwnerAsync(chest, ChestFullNotification, null);
    }

    /// <summary>The player's own items that may be traded to wired: in their inventory, and tradeable.</summary>
    private static Task<List<UserItem>> AvailableForWiredTradeAsync(Habbo.Infrastructure.AppDbContext db, Guid playerId, List<int> itemIds) =>
        db.UserItems.AsNoTracking().Include(i => i.Definition)
            .Where(i => itemIds.Contains(i.Id) && i.OwnerId == playerId && i.RoomId == null && i.ChestId == null && !i.InMarketplace
                        && i.Tradeable && i.Definition!.Tradeable)
            .ToListAsync();

    /// <summary>What the player offers, against what wired offers; a deposit may be accepted once it holds something, a
    /// contract's offer once it meets the rules, and the times it meets them go along.</summary>
    private void SendWiredTradeItems(RoomEntityStore.RoomEntity user, WiredTrade trade, bool? acceptable = null)
    {
        var canAccept = acceptable ?? trade.Items.Count > 0;
        var extra = trade.Terms is null ? (canAccept ? 1 : 0) : trade.TimesMet;
        SendTo(user.Player.Id, new WiredTradeItemsUpdateMessageComposer(
            user.Player.AccountId, trade.Items.Values.ToList(), trade.Items.Count, trade.Credits,
            WiredSide, trade.ReservedView.ToList(), trade.ReservedView.Count, trade.ReservedCredits, canAccept, extra));
    }

    /// <summary>Credit furni as the trade window tells it apart: by its class name.</summary>
    private const string CreditFurniPrefix = "CF_";

    private static bool IsCreditFurni(ItemDefinition definition) => definition.ClassName.StartsWith(CreditFurniPrefix, StringComparison.Ordinal);

    /// <summary>The account id the trade window gives wired's side.</summary>
    private const int WiredSide = 0;

    /// <summary>Leaving the room calls off a wired trade.</summary>
    private async Task LeaveWiredTradeAsync(Guid playerId)
    {
        if (!_wiredTrades.TryGetValue(playerId, out var trade))
            return;

        if (trade.Terms is not null)
            await EndContractTradeAsync(trade, WiredTransactionFailure.TradeCancelled);
        else
            EndWiredTrade(trade, WiredTransactionFailure.TradeCancelled);
    }
}

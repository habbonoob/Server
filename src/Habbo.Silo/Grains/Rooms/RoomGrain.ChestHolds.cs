using System.Globalization;
using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Chests;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Transactions;
using Habbo.Protocol.Messages.Outgoing.Vault;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Chests;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>What the room's chests hold, kept in step with the database so wired can count without waiting on it,
/// and what wired may do with them: count them, and give from them.</summary>
public sealed partial class RoomGrain
{
    /// <summary>How wired picks the items it gives: the chest's random order, oldest first, or newest first.</summary>
    private const int RandomIteration = 0;
    private const int FirstInFirstOut = 1;
    private const int LastInFirstOut = 2;

    /// <summary>The chest notification for any change wired made.</summary>
    private const string ChestWiredTransactionNotification = "wired_chests.chest_wired_transaction";

    private readonly Dictionary<int, ChestHold> _chestHolds = [];

    // what the wired access below reaches through the grain
    private Microsoft.EntityFrameworkCore.IDbContextFactory<AppDbContext> ChestDb => dbFactory;

    /// <summary>Reads what every chest in the room holds.</summary>
    private async Task LoadChestHoldsAsync(AppDbContext db)
    {
        _chestHolds.Clear();
        var chestIds = _items!.Items.Where(i => i.Interaction is ChestInteraction).Select(i => i.Item.Id).ToList();
        if (chestIds.Count == 0)
            return;

        var coins = await db.ChestCoins.AsNoTracking().Where(c => chestIds.Contains(c.ItemId)).ToDictionaryAsync(c => c.ItemId, c => c.Coins);
        var items = await db.UserItems.AsNoTracking().Include(i => i.Definition).Where(i => i.ChestId != null && chestIds.Contains(i.ChestId.Value)).ToListAsync();
        var data = await furniData.LoadAsync(db, items);
        foreach (var chestId in chestIds)
            _chestHolds[chestId] = new ChestHold { Coins = coins.GetValueOrDefault(chestId) };
        foreach (var item in items)
            _chestHolds[item.ChestId!.Value].Items.Add(Held(item, data[item.Id]));
    }

    private async Task LoadChestHoldAsync(AppDbContext db, int chestId)
    {
        var hold = new ChestHold { Coins = await CoinsInAsync(db, chestId) };
        var items = await db.UserItems.AsNoTracking().Include(i => i.Definition).Where(i => i.ChestId == chestId).ToListAsync();
        var data = await furniData.LoadAsync(db, items);
        hold.Items.AddRange(items.Select(i => Held(i, data[i.Id])));
        _chestHolds[chestId] = hold;
    }

    private static HeldItem Held(UserItem item, FurniDataView data) =>
        new(item.Id, KindOf(item, data), item.StoredAt ?? item.CreatedAt, item.ChestDrawOrder ?? 0);

    private static ChestKind KindOf(UserItem item, FurniDataView data) =>
        new(item.Definition!.Type == ItemType.Wall, item.Definition.ClassId, PosterOf(item, data));

    /// <summary>A chest taken out of the room is no longer the room's to keep track of.</summary>
    private void ForgetChest(int itemId)
    {
        _chestHolds.Remove(itemId);
        _chestViewers.Remove(itemId);
    }

    private ChestHold HoldOf(int chestId) => _chestHolds.TryGetValue(chestId, out var hold) ? hold : _chestHolds[chestId] = new ChestHold();

    /// <summary>Wired's hands on the room's chests.</summary>
    private sealed class WiredChestAccess(RoomGrain room) : IWiredChests
    {
        public bool IsUsable(RoomItemStore.PlacedItem item) =>
            item.Interaction is ChestInteraction && ChestSettings.Read(item.Data) is { WiredEnabled: true, Locked: false };

        public ChestKind KindOf(RoomItemStore.PlacedItem item) =>
            new(item.Item.Definition!.Type == ItemType.Wall, item.Item.Definition.ClassId, PosterOf(item.Item, item.Data));

        public int Count(RoomItemStore.PlacedItem chest, IReadOnlySet<ChestKind>? kinds = null, bool previewedOnly = false)
        {
            if (!IsUsable(chest))
                return 0;

            var hold = room.HoldOf(chest.Item.Id);
            if (IsCoinChest(chest))
                return hold.Coins;
            if (previewedOnly)
            {
                var shown = ChestSettings.Read(chest.Data).Visuals.Split(PreviewSeparator, StringSplitOptions.RemoveEmptyEntries)
                    .Select(ChestKind.FromPreview).OfType<ChestKind>();
                return kinds is null ? shown.Count() : shown.Count(kinds.Contains);
            }

            return kinds is null ? hold.Items.Count : hold.Items.Count(i => kinds.Contains(i.Kind));
        }

        public async Task GiveCoinsAsync(IReadOnlyList<RoomItemStore.PlacedItem> chests, RoomEntityStore.RoomEntity user, int? amount,
            byte earningsCategory, WiredChestGift gift)
        {
            var usable = chests.Where(c => IsUsable(c) && IsCoinChest(c)).ToList();
            if (usable.Count == 0)
            {
                Fail(user, WiredTransactionFailure.NoOrLockedChests);
                return;
            }

            var wanted = amount ?? usable.Sum(c => room.HoldOf(c.Item.Id).Coins);
            if (wanted <= 0 || usable.Sum(c => room.HoldOf(c.Item.Id).Coins) < wanted)
            {
                Fail(user, WiredTransactionFailure.InsufficientFunds);
                return;
            }

            await using var db = await room.ChestDb.CreateDbContextAsync();
            await using var dbTransaction = await db.Database.BeginTransactionAsync();
            var transaction = room.NewChestTransaction(ChestTransaction.Wired, Info(gift, wanted), user.Player.Id, []);
            var left = wanted;
            var takenFrom = new List<(RoomItemStore.PlacedItem Chest, int Coins)>();
            foreach (var chest in usable.Where(_ => left > 0))
            {
                var chestId = chest.Item.Id;
                var taken = Math.Min(left, room.HoldOf(chestId).Coins);
                if (taken == 0)
                    continue;

                // the database has the last word on what the chest holds
                if (await db.ChestCoins.Where(c => c.ItemId == chestId && c.Coins >= taken)
                        .ExecuteUpdateAsync(s => s.SetProperty(c => c.Coins, c => c.Coins - taken)) == 0)
                {
                    await dbTransaction.RollbackAsync();
                    Fail(user, WiredTransactionFailure.FundsNoLongerAvailable);
                    return;
                }

                left -= taken;
                transaction.ChestIds.Add(chestId);
                transaction.WithdrawnCoins += taken;
                takenFrom.Add((chest, taken));
            }

            // credits go to the earnings window, where the user claims them
            var afterCommit = new List<Func<Task>>();
            room.Earn(db, user.Player.Id, earningsCategory, wanted, afterCommit);
            db.ChestTransactions.Add(transaction);
            await db.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            var emptied = new List<RoomItemStore.PlacedItem>();
            foreach (var (chest, taken) in takenFrom)
            {
                var hold = room.HoldOf(chest.Item.Id);
                hold.Coins -= taken;
                room.BroadcastToChestViewers(chest.Item.Id, new CoinsChestContentsMessageComposer(chest.Item.Id, hold.Coins, true));
                if (hold.Coins == 0)
                    emptied.Add(chest);
            }

            foreach (var step in afterCommit)
                await step();
            await room.PruneChestLogsAsync(db);
            room.GrainFactory.GetGrain<IPlayerGrain>(user.Player.Id).ProgressAchievementAsync(AchievementNames.GamerPaycheck, wanted).Ignore();
            room.SendTo(user.Player.Id, Rewarded(gift, [new TradeRequirementNodeData(TradeRequirementNodeData.Coins, wanted, null)]));
            await AfterWiredChangeAsync(usable.Where(c => transaction.ChestIds.Contains(c.Item.Id)).ToList(), emptied);
        }

        public async Task GiveItemsAsync(IReadOnlyList<RoomItemStore.PlacedItem> chests, RoomEntityStore.RoomEntity user, int? amount,
            int iteration, WiredChestGift gift)
        {
            var usable = chests.Where(c => IsUsable(c) && !IsCoinChest(c)).ToList();
            if (usable.Count == 0)
            {
                Fail(user, WiredTransactionFailure.NoOrLockedChests);
                return;
            }

            var held = usable.Sum(c => room.HoldOf(c.Item.Id).Items.Count);
            var wanted = amount ?? held;
            if (wanted <= 0 || held < wanted)
            {
                Fail(user, WiredTransactionFailure.InsufficientFunds);
                return;
            }

            // the chests give in the order they come, each its items in the iteration's order
            var picked = new List<(RoomItemStore.PlacedItem Chest, HeldItem Item)>();
            foreach (var chest in usable.Where(_ => picked.Count < wanted))
            {
                var ordered = iteration switch
                {
                    FirstInFirstOut => room.HoldOf(chest.Item.Id).Items.OrderBy(i => i.StoredAt),
                    LastInFirstOut => room.HoldOf(chest.Item.Id).Items.OrderByDescending(i => i.StoredAt),
                    _ => room.HoldOf(chest.Item.Id).Items.OrderBy(i => i.DrawOrder),
                };
                picked.AddRange(ordered.Take(wanted - picked.Count).Select(i => (chest, i)));
            }

            await using var db = await room.ChestDb.CreateDbContextAsync();
            var ids = picked.Select(p => p.Item.ItemId).ToList();
            var items = await db.UserItems.Include(i => i.Definition).Where(i => ids.Contains(i.Id) && i.ChestId != null).ToListAsync();
            if (items.Count != ids.Count)
            {
                Fail(user, WiredTransactionFailure.FundsNoLongerAvailable);
                return;
            }

            var touched = picked.Select(p => p.Chest).Distinct().ToList();
            var transaction = room.NewChestTransaction(ChestTransaction.Wired, Info(gift, wanted), user.Player.Id, touched.Select(c => c.Item.Id).ToList());
            transaction.WithdrawnFurni = items.Count;
            await room.AddKindsAsync(db, transaction, items, deposited: false);
            db.ChestTransactions.Add(transaction);
            foreach (var item in items)
            {
                item.ChestId = null;
                item.StoredAt = null;
                item.StoredInTransactionId = null;
                item.ChestDrawOrder = null;
                item.OwnerId = user.Player.Id;
            }

            await db.SaveChangesAsync();
            await room.PruneChestLogsAsync(db);

            var emptied = new List<RoomItemStore.PlacedItem>();
            foreach (var chest in touched)
            {
                var hold = room.HoldOf(chest.Item.Id);
                var gone = picked.Where(p => p.Chest == chest).Select(p => p.Item).ToList();
                hold.Items.RemoveAll(gone.Contains);
                room.BroadcastToChestViewers(chest.Item.Id, new ItemsChestContentsUpdatedMessageComposer(chest.Item.Id, gone.Select(i => i.ItemId).ToList(), []));
                if (hold.Items.Count == 0)
                    emptied.Add(chest);
            }

            await room.GrainFactory.GetGrain<IPlayerGrain>(user.Player.Id).AnnounceItemsAsync(ids);
            var given = picked.GroupBy(p => p.Item.Kind)
                .Select(g => new TradeRequirementNodeData(TradeRequirementNodeData.Furni, g.Count(),
                    new Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests.Storage.ChestItemTypeData(g.Key.IsWallItem, g.Key.ClassId, g.Key.PosterId)))
                .ToList();
            room.SendTo(user.Player.Id, Rewarded(gift, given));
            await AfterWiredChangeAsync(touched, emptied);
        }

        public async Task<WiredContract?> ContractOfAsync(RoomItemStore.PlacedItem item)
        {
            if (item.Interaction is not ContractInteraction)
                return null;

            await using var db = await room.ChestDb.CreateDbContextAsync();
            return await RoomGrain.ContractOfAsync(db, item);
        }

        public Task StartContractAsync(RoomEntityStore.RoomEntity user, ContractTerms terms, int timeoutSeconds) =>
            room.StartContractAsync(user, terms, timeoutSeconds);

        public Task CancelContractsAsync(RoomEntityStore.RoomEntity user, IReadOnlySet<int>? contractIds) =>
            room.CancelContractTradesAsync(user, contractIds);

        public void Fail(RoomEntityStore.RoomEntity user, int failure) =>
            room.SendTo(user.Player.Id, new WiredTransactionFailMessageComposer(failure));

        /// <summary>The chests look as they now hold, and their owners hear what wired did, and which it emptied.</summary>
        private async Task AfterWiredChangeAsync(List<RoomItemStore.PlacedItem> touched, List<RoomItemStore.PlacedItem> emptied)
        {
            foreach (var chest in touched)
            {
                await room.RefreshChestLookAsync(chest);
                var settings = ChestSettings.Read(chest.Data);
                if (settings.NotifyWiredTransaction)
                    await room.NotifyChestOwnerAsync(chest, ChestWiredTransactionNotification, null);
                if (settings.NotifyChestEmpty && emptied.Contains(chest))
                    await room.NotifyChestOwnerAsync(chest, ChestEmptyNotification, null);
            }
        }

        private static WiredTransactionSuccessMessageComposer Rewarded(WiredChestGift gift, List<TradeRequirementNodeData> given) =>
            new(WiredTransactionSuccessMessageComposer.Rewarded, new TradeRequirementRuleData(given), gift.RewardText, gift.OpenByDefault);

        /// <summary>What the log keeps for a wired transaction: the effect that gave, and how much.</summary>
        private static string Info(WiredChestGift gift, int amount) =>
            string.Create(CultureInfo.InvariantCulture, $"{gift.EffectItemId} {amount}");
    }
}

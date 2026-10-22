using System.Globalization;
using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Chests;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests.Storage;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Transactions;
using Habbo.Protocol.Messages.Outgoing.Vault;
using Habbo.Silo.Chests;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Transactions under contracts, which wired starts for a user. A reward gives at once. A payment or a trade
/// opens the trade window with the contract's rules: the user offers until what they offer meets a rule as often as the
/// contract asks, and for a trade wired sets aside what it gives in return. On confirming, the payment goes into the
/// contract's chests — past their capacity, to the chest's owner — and a trade's return comes out of them. The contract's
/// triggers hear how it ended.</summary>
public sealed partial class RoomGrain
{
    /// <summary>Where credits from a chest land in a user's earnings when nothing names a category.</summary>
    private const byte ChestEarnings = IncomeRewardData.WiredChests;

    /// <summary>Starts a transaction for the user; one they have going is replaced.</summary>
    private async Task StartContractAsync(RoomEntityStore.RoomEntity user, ContractTerms terms, int timeoutSeconds)
    {
        var playerId = user.Player.Id;
        if (_items?.Find(terms.ContractItemId) is not { } contractFurni)
            return;
        if (user.Player.TradeLocked)
        {
            await FailContractAsync(user, contractFurni, WiredTransactionFailure.UserCannotTrade);
            return;
        }

        if (_trades.ContainsKey(playerId))
        {
            await FailContractAsync(user, contractFurni, WiredTransactionFailure.AlreadyTrading);
            return;
        }

        if (terms.Contract.Type == WiredContract.Reward)
        {
            await GiveContractRewardAsync(user, contractFurni, terms);
            return;
        }

        var replacing = _wiredTrades.Remove(playerId);
        var requirement = terms.IsDonation ? TradeRequirementData.DonateAnything : TradeRequirementData.ContractRules;
        var trade = new WiredTrade(playerId, 0, requirement, terms);
        _wiredTrades[playerId] = trade;

        var contract = terms.Contract;
        var rules = terms.IsDonation ? null : new TradeRequirementRulesData(ContractWire.Definition(contract), terms.RuleType,
            terms.RuleType == ContractTerms.Multiplier ? terms.MaxTimes : null, terms.RuleType == ContractTerms.AutoMultiplier ? terms.MaxTimes : null);
        SendTo(playerId, new WiredTradeInitiateMessageComposer(
            new TradeRequirementData(requirement, contract.Type == WiredContract.Payment ? contract.ReceiveText : string.Empty,
                contract.Type == WiredContract.Payment ? contract.LayoutType : string.Empty, rules),
            ShowRequirementsImmediate: true, OverridePreviousTrade: replacing, TimeoutSeconds: timeoutSeconds));

        if (timeoutSeconds > 0)
        {
            Schedule(TimeSpan.FromSeconds(timeoutSeconds), async () =>
            {
                if (_wiredTrades.TryGetValue(playerId, out var running) && running.Id == trade.Id)
                    await EndContractTradeAsync(running, WiredTransactionFailure.Timeout);
            });
        }
    }

    /// <summary>Adds to or takes from what the user offers under the contract, as long as it can still meet a rule.</summary>
    private async Task ChangeContractOfferAsync(RoomEntityStore.RoomEntity user, WiredTrade trade, IReadOnlyList<int> itemIds)
    {
        var terms = trade.Terms!;
        var wanted = itemIds.Distinct().Where(id => !trade.Items.ContainsKey(id)).ToList();
        await using var db = await dbFactory.CreateDbContextAsync();
        var items = (await AvailableForWiredTradeAsync(db, user.Player.Id, wanted))
            .Where(i => interactions.For(i.Definition!) is not ChestInteraction).ToList();
        if (items.Count < wanted.Count)
            SendTo(user.Player.Id, new WiredTradeTransactionNotificationMessageComposer(WiredTradeTransactionNotificationMessageComposer.InvalidItem));
        if (items.Count == 0)
            return;

        var data = await furniData.LoadAsync(db, items);
        var credits = new Dictionary<int, int>(trade.CreditValues);
        var kinds = new Dictionary<int, ChestKind>(trade.Kinds);
        foreach (var item in items)
        {
            if (IsCreditFurni(item.Definition!))
                credits[item.Id] = item.Definition!.CreditValue ?? 0;
            else
                kinds[item.Id] = KindOf(item, data[item.Id]);
        }

        // nothing goes in that cannot be part of meeting a rule, and nothing once the rules are met in full
        var offer = OfferOf(credits, kinds);
        if (!offer.Fits(terms))
        {
            var (times, acceptable) = OfferOf(trade.CreditValues, trade.Kinds).Measure(terms);
            var full = acceptable && (terms.RuleType != ContractTerms.AutoMultiplier || times == terms.MaxTimes);
            SendTo(user.Player.Id, new WiredTradeTransactionNotificationMessageComposer(full
                ? WiredTradeTransactionNotificationMessageComposer.RequirementsAlreadyMet
                : WiredTradeTransactionNotificationMessageComposer.ExceedsOffer));
            return;
        }

        foreach (var item in items)
        {
            trade.Items[item.Id] = ToTradeItem(item, data[item.Id]);
            if (credits.TryGetValue(item.Id, out var value))
                trade.CreditValues[item.Id] = value;
            else
                trade.Kinds[item.Id] = kinds[item.Id];
        }

        await RemeasureContractOfferAsync(db, user, trade);
    }

    /// <summary>Works out how often the offer meets the rules, sets aside a trade's return for that, and shows it all.</summary>
    private async Task RemeasureContractOfferAsync(AppDbContext db, RoomEntityStore.RoomEntity user, WiredTrade trade)
    {
        var terms = trade.Terms!;
        var (times, acceptable) = OfferOf(trade.CreditValues, trade.Kinds).Measure(terms);
        trade.TimesMet = times;
        trade.Reserved.Clear();
        trade.ReservedView.Clear();
        trade.ReservedCredits = 0;

        if (acceptable && terms.Contract.Type == WiredContract.Trade && terms.Contract.YouGet is { } youGet
            && !await ReserveAsync(db, trade, youGet, times, user))
            acceptable = false;

        SendWiredTradeItems(user, trade, acceptable);
    }

    /// <summary>Sets aside what wired gives for the offer: the rule's credits and items, so many times, from the chests.
    /// False, and the user told, when the chests do not hold enough.</summary>
    private async Task<bool> ReserveAsync(AppDbContext db, WiredTrade trade, ContractRule youGet, int times, RoomEntityStore.RoomEntity user)
    {
        var (credits, kinds) = ContractOffer.Needs(youGet);
        var chests = UsableChests(trade.Terms!.ChestIds);
        var taken = _wiredTrades.Values.Where(t => t != trade).SelectMany(t => t.Reserved).Select(r => r.Item.ItemId).ToHashSet();

        var wantedCredits = credits * times;
        if (chests.Where(IsCoinChest).Sum(c => HoldOf(c.Item.Id).Coins) - ReservedCreditsOf(trade) < wantedCredits)
        {
            SendTo(user.Player.Id, new WiredTradeTransactionNotificationMessageComposer(WiredTradeTransactionNotificationMessageComposer.TooManyWiredCredits));
            return false;
        }

        foreach (var (kind, count) in kinds)
        {
            var available = chests.Where(c => !IsCoinChest(c))
                .SelectMany(c => HoldOf(c.Item.Id).Items.Where(i => i.Kind == kind && !taken.Contains(i.ItemId)).OrderBy(i => i.DrawOrder)
                    .Select(i => (c.Item.Id, i)))
                .Take(count * times).ToList();
            if (available.Count < count * times)
            {
                SendTo(user.Player.Id, new WiredTradeTransactionNotificationMessageComposer(WiredTradeTransactionNotificationMessageComposer.TooManyWiredItems));
                trade.Reserved.Clear();
                return false;
            }

            trade.Reserved.AddRange(available);
        }

        trade.ReservedCredits = wantedCredits;
        var ids = trade.Reserved.Select(r => r.Item.ItemId).ToList();
        var items = await db.UserItems.AsNoTracking().Include(i => i.Definition).Where(i => ids.Contains(i.Id)).ToListAsync();
        var data = await furniData.LoadAsync(db, items);
        trade.ReservedView.AddRange(items.Select(i => ToTradeItem(i, data[i.Id])));
        return true;
    }

    private int ReservedCreditsOf(WiredTrade except) => _wiredTrades.Values.Where(t => t != except).Sum(t => t.ReservedCredits);

    /// <summary>Makes the trade under the contract, now that the user has confirmed it.</summary>
    private async Task CompleteContractTradeAsync(RoomEntityStore.RoomEntity user, WiredTrade trade)
    {
        var terms = trade.Terms!;
        if (_items?.Find(terms.ContractItemId) is not { } contractFurni)
        {
            SendTo(user.Player.Id, new WiredTradeCancelledMessageComposer(WiredTransactionFailure.WiredMisconfiguration));
            return;
        }

        var (times, acceptable) = OfferOf(trade.CreditValues, trade.Kinds).Measure(terms);
        if (!acceptable)
        {
            await EndContractTradeAsync(trade, WiredTransactionFailure.InvalidTrade);
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var offered = await db.UserItems.Include(i => i.Definition)
            .Where(i => trade.Items.Keys.Contains(i.Id) && i.OwnerId == user.Player.Id && i.RoomId == null && i.ChestId == null && !i.InMarketplace)
            .ToListAsync();
        var reservedIds = trade.Reserved.Select(r => r.Item.ItemId).ToList();
        var returned = await db.UserItems.Include(i => i.Definition).Where(i => reservedIds.Contains(i.Id) && i.ChestId != null).ToListAsync();
        var chests = UsableChests(terms.ChestIds);
        if (offered.Count != trade.Items.Count || !StillReserved(trade, returned, chests)
            || chests.Where(IsCoinChest).Sum(c => HoldOf(c.Item.Id).Coins) < trade.ReservedCredits)
        {
            await EndContractTradeAsync(trade, WiredTransactionFailure.FundsNoLongerAvailable);
            return;
        }

        var credits = offered.Where(i => IsCreditFurni(i.Definition!)).Sum(i => i.Definition!.CreditValue ?? 0);
        var furni = offered.Where(i => !IsCreditFurni(i.Definition!)).ToList();
        if ((credits > 0 && !chests.Any(IsCoinChest)) || (furni.Count > 0 && !chests.Any(c => !IsCoinChest(c))))
        {
            await EndContractTradeAsync(trade, WiredTransactionFailure.NoOrLockedChests);
            return;
        }

        // the offered items are claimed with one conditional write before anything is paid: read as free above, one may
        // have been listed, placed or traded from elsewhere since. The write also holds their rows until the commit.
        var offeredIds = offered.Select(i => i.Id).ToList();
        await using var dbTransaction = await db.Database.BeginTransactionAsync();
        if (await db.UserItems.Where(i => offeredIds.Contains(i.Id) && i.OwnerId == user.Player.Id && i.RoomId == null && i.ChestId == null && !i.InMarketplace)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.OwnerId, i => i.OwnerId)) != offeredIds.Count)
        {
            await dbTransaction.RollbackAsync();
            await EndContractTradeAsync(trade, WiredTransactionFailure.FundsNoLongerAvailable);
            return;
        }

        _wiredTrades.Remove(trade.PlayerId);
        var type = terms.Contract.Type == WiredContract.Trade ? ChestTransaction.ContractTrade : ChestTransaction.ContractPayment;
        var transaction = NewChestTransaction(type, ContractInfo(terms, times), user.Player.Id, []);
        db.ChestTransactions.Add(transaction);
        await AddKindsAsync(db, transaction, furni, deposited: true);
        await AddKindsAsync(db, transaction, returned, deposited: false);
        transaction.DepositedFurni = furni.Count;
        transaction.DepositedCoins = credits;
        transaction.WithdrawnFurni = returned.Count;
        transaction.WithdrawnCoins = trade.ReservedCredits;
        await db.SaveChangesAsync();

        // what the room keeps in memory and tells anyone changes only once the trade has committed
        var touched = new HashSet<RoomItemStore.PlacedItem>();
        var afterCommit = new List<Func<Task>>();
        await PayIntoChestsAsync(db, chests, furni, credits, transaction, touched, afterCommit);

        // credit furni has become the credits it is worth
        db.UserItems.RemoveRange(offered.Where(i => IsCreditFurni(i.Definition!)));
        if (!await GiveFromChestsAsync(db, chests, user, trade, transaction, touched, afterCommit))
        {
            await dbTransaction.RollbackAsync();
            await EndContractTradeAsync(trade, WiredTransactionFailure.FundsNoLongerAvailable);
            return;
        }

        await db.SaveChangesAsync();
        await dbTransaction.CommitAsync();
        foreach (var step in afterCommit)
            await step();
        await PruneChestLogsAsync(db);

        // a user paid credits into the owner's game
        if (type == ChestTransaction.ContractPayment && credits > 0 && user.Player.Id != _room!.OwnerId)
            GrainFactory.GetGrain<IPlayerGrain>(_room.OwnerId).ProgressAchievementAsync(AchievementNames.MoneyPrinter, 1).Ignore();

        SendTo(user.Player.Id, FurniListRemoveMultipleComposer.Items(offered.Select(i => i.Id)));
        if (returned.Count > 0)
            await GrainFactory.GetGrain<IPlayerGrain>(user.Player.Id).AnnounceItemsAsync(returned.Select(i => i.Id).ToList());
        SendTo(user.Player.Id, new WiredTradeCompletedMessageComposer());
        SendTo(user.Player.Id, WiredTransactionSuccessMessageComposer.Of(type == ChestTransaction.ContractTrade
            ? WiredTransactionSuccessMessageComposer.Traded
            : WiredTransactionSuccessMessageComposer.Paid));

        foreach (var chest in touched)
        {
            await RefreshChestLookAsync(chest);
            if (ChestSettings.Read(chest.Data).NotifyWiredTransaction)
                await NotifyChestOwnerAsync(chest, ChestWiredTransactionNotification, null);
        }

        if (_wired is not null)
            await _wired.TransactionCompletedAsync(user, contractFurni);
    }

    /// <summary>Puts a payment into the chests in their order, up to what each may hold; what does not fit goes to the
    /// owner of the first chest of its sort — furniture into their inventory, credits into their earnings.</summary>
    private async Task PayIntoChestsAsync(AppDbContext db, List<RoomItemStore.PlacedItem> chests, List<UserItem> furni, int credits,
        ChestTransaction transaction, HashSet<RoomItemStore.PlacedItem> touched, List<Func<Task>> afterCommit)
    {
        var now = DateTimeOffset.UtcNow;
        var left = new Queue<UserItem>(furni);
        var furniChests = chests.Where(c => !IsCoinChest(c)).ToList();
        foreach (var chest in furniChests)
        {
            var hold = HoldOf(chest.Item.Id);
            var room = await ChestCapacityAsync(chest) - hold.Items.Count;
            var stored = new List<UserItem>();
            while (room-- > 0 && left.Count > 0)
                stored.Add(left.Dequeue());
            if (stored.Count == 0)
                continue;

            foreach (var item in stored)
            {
                item.ChestId = chest.Item.Id;
                item.OwnerId = chest.Item.OwnerId;
                item.StoredAt = now;
                item.StoredInTransactionId = transaction.Id;
                item.ChestDrawOrder = _chestRandom.Next();
            }

            var data = await furniData.LoadAsync(db, stored);
            afterCommit.Add(() =>
            {
                hold.Items.AddRange(stored.Select(i => Held(i, data[i.Id])));
                BroadcastToChestViewers(chest.Item.Id, new ItemsChestContentsUpdatedMessageComposer(chest.Item.Id, [], stored.Select(i => ToStorage(i, data[i.Id])).ToList()));
                return Task.CompletedTask;
            });
            transaction.ChestIds.Add(chest.Item.Id);
            touched.Add(chest);
        }

        if (left.Count > 0 && furniChests.FirstOrDefault() is { } overflowChest)
        {
            var overflow = left.ToList();
            foreach (var item in overflow)
                item.OwnerId = overflowChest.Item.OwnerId;
            await db.SaveChangesAsync();
            afterCommit.Add(async () =>
            {
                await GrainFactory.GetGrain<IPlayerGrain>(overflowChest.Item.OwnerId).AnnounceItemsAsync(overflow.Select(i => i.Id).ToList());
                await NotifyChestOwnerAsync(overflowChest, ChestEarningsNotification, null);
            });
        }

        var creditsLeft = credits;
        var coinChests = chests.Where(IsCoinChest).ToList();
        foreach (var chest in coinChests.Where(_ => creditsLeft > 0))
        {
            var hold = HoldOf(chest.Item.Id);
            var stored = Math.Min(creditsLeft, Math.Max(0, await ChestCapacityAsync(chest) - hold.Coins));
            if (stored == 0)
                continue;

            var row = await db.ChestCoins.FirstOrDefaultAsync(c => c.ItemId == chest.Item.Id);
            if (row is null)
                db.ChestCoins.Add(row = new ChestCoins { ItemId = chest.Item.Id });
            row.Coins += stored;
            creditsLeft -= stored;
            afterCommit.Add(() =>
            {
                hold.Coins += stored;
                BroadcastToChestViewers(chest.Item.Id, new CoinsChestContentsMessageComposer(chest.Item.Id, hold.Coins, true));
                return Task.CompletedTask;
            });
            if (!transaction.ChestIds.Contains(chest.Item.Id))
                transaction.ChestIds.Add(chest.Item.Id);
            touched.Add(chest);
        }

        if (creditsLeft > 0 && coinChests.FirstOrDefault() is { } coinOverflow)
        {
            Earn(db, coinOverflow.Item.OwnerId, ChestEarnings, creditsLeft, afterCommit);
            afterCommit.Add(() => NotifyChestOwnerAsync(coinOverflow, ChestEarningsNotification, null));
        }
    }

    /// <summary>Hands the user what wired set aside for the trade: its items, and its credits into their earnings. False,
    /// and nothing should be saved, when the chests no longer hold it.</summary>
    private async Task<bool> GiveFromChestsAsync(AppDbContext db, List<RoomItemStore.PlacedItem> chests, RoomEntityStore.RoomEntity user,
        WiredTrade trade, ChestTransaction transaction, HashSet<RoomItemStore.PlacedItem> touched, List<Func<Task>> afterCommit)
    {
        var tracked = db.ChangeTracker.Entries<UserItem>().Select(e => e.Entity).ToDictionary(i => i.Id);
        foreach (var group in trade.Reserved.GroupBy(r => r.ChestId))
        {
            // an item leaves only the chest it was set aside from, and only while wired may still use that chest
            if (chests.FirstOrDefault(c => c.Item.Id == group.Key) is not { } chest)
                return false;

            var chestId = group.Key;
            var groupIds = group.Select(r => r.Item.ItemId).ToList();
            foreach (var id in groupIds)
            {
                if (!tracked.TryGetValue(id, out var item) || item.ChestId != chestId)
                    return false;

                item.ChestId = null;
                item.StoredAt = null;
                item.StoredInTransactionId = null;
                item.ChestDrawOrder = null;
                item.OwnerId = user.Player.Id;
            }

            afterCommit.Add(() =>
            {
                HoldOf(chestId).Items.RemoveAll(i => groupIds.Contains(i.ItemId));
                BroadcastToChestViewers(chestId, new ItemsChestContentsUpdatedMessageComposer(chestId, groupIds, []));
                return Task.CompletedTask;
            });
            if (!transaction.ChestIds.Contains(group.Key))
                transaction.ChestIds.Add(group.Key);
            touched.Add(chest);
        }

        var left = trade.ReservedCredits;
        foreach (var chest in chests.Where(IsCoinChest).Where(_ => left > 0))
        {
            var hold = HoldOf(chest.Item.Id);
            var taken = Math.Min(left, hold.Coins);
            if (taken == 0)
                continue;

            // the database has the last word on what the chest holds
            var row = await db.ChestCoins.FirstOrDefaultAsync(c => c.ItemId == chest.Item.Id);
            if (row is null || row.Coins < taken)
                return false;

            row.Coins -= taken;
            left -= taken;
            afterCommit.Add(() =>
            {
                hold.Coins -= taken;
                BroadcastToChestViewers(chest.Item.Id, new CoinsChestContentsMessageComposer(chest.Item.Id, hold.Coins, true));
                return Task.CompletedTask;
            });
            if (!transaction.ChestIds.Contains(chest.Item.Id))
                transaction.ChestIds.Add(chest.Item.Id);
            touched.Add(chest);
        }

        if (left > 0)
            return false;

        Earn(db, user.Player.Id, ChestEarnings, trade.ReservedCredits, afterCommit);
        return true;
    }

    /// <summary>Puts credits into a player's earnings with the caller's own save; they are told once it has committed.</summary>
    private void Earn(AppDbContext db, Guid playerId, byte category, int credits, List<Func<Task>> afterCommit)
    {
        if (credits <= 0)
            return;

        db.IncomeRewards.Add(new IncomeReward { UserId = playerId, Category = category, RewardType = IncomeRewardData.Credits, Amount = credits });
        afterCommit.Add(() => earnings.NotifyAsync(playerId, category));
    }

    /// <summary>Whether every item set aside for the trade is still in the chest it was set aside from, and that chest
    /// one wired may still use.</summary>
    private static bool StillReserved(WiredTrade trade, List<UserItem> items, List<RoomItemStore.PlacedItem> chests)
    {
        var byId = items.ToDictionary(i => i.Id);
        return trade.Reserved.All(r => byId.TryGetValue(r.Item.ItemId, out var item) && item.ChestId == r.ChestId
            && chests.Any(c => c.Item.Id == r.ChestId));
    }

    /// <summary>A reward contract gives at once: its items and credits, so many times, from the chests, with its pop-up.</summary>
    private async Task GiveContractRewardAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem contractFurni, ContractTerms terms)
    {
        if (terms.Contract.YouGet is not { Nodes.Count: > 0 } youGet)
        {
            await FailContractAsync(user, contractFurni, WiredTransactionFailure.WiredMisconfiguration);
            return;
        }

        var trade = new WiredTrade(user.Player.Id, 0, TradeRequirementData.ContractRules, terms);
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await ReserveAsync(db, trade, youGet, terms.MaxTimes, user))
        {
            await FailContractAsync(user, contractFurni, WiredTransactionFailure.InsufficientFunds);
            return;
        }

        var reservedIds = trade.Reserved.Select(r => r.Item.ItemId).ToList();
        var given = await db.UserItems.Include(i => i.Definition).Where(i => reservedIds.Contains(i.Id) && i.ChestId != null).ToListAsync();
        var transaction = NewChestTransaction(ChestTransaction.ContractReward, ContractInfo(terms, terms.MaxTimes), user.Player.Id, []);
        await AddKindsAsync(db, transaction, given, deposited: false);
        transaction.WithdrawnFurni = given.Count;
        transaction.WithdrawnCoins = trade.ReservedCredits;
        db.ChestTransactions.Add(transaction);

        // what the room keeps in memory and tells anyone changes only once the reward is saved
        var touched = new HashSet<RoomItemStore.PlacedItem>();
        var afterCommit = new List<Func<Task>>();
        var chests = UsableChests(terms.ChestIds);
        var credits = trade.ReservedCredits;
        trade.ReservedCredits = 0;
        if (!StillReserved(trade, given, chests) || !await GiveFromChestsAsync(db, chests, user, trade, transaction, touched, afterCommit))
        {
            await FailContractAsync(user, contractFurni, WiredTransactionFailure.FundsNoLongerAvailable);
            return;
        }

        // a reward's credits land in the category the contract names
        var left = credits;
        foreach (var chest in chests.Where(IsCoinChest).Where(_ => left > 0))
        {
            var hold = HoldOf(chest.Item.Id);
            var taken = Math.Min(left, hold.Coins);
            if (taken == 0)
                continue;

            // the database has the last word on what the chest holds
            var row = await db.ChestCoins.FirstOrDefaultAsync(c => c.ItemId == chest.Item.Id);
            if (row is null || row.Coins < taken)
                break;

            row.Coins -= taken;
            left -= taken;
            afterCommit.Add(() =>
            {
                hold.Coins -= taken;
                BroadcastToChestViewers(chest.Item.Id, new CoinsChestContentsMessageComposer(chest.Item.Id, hold.Coins, true));
                return Task.CompletedTask;
            });
            if (!transaction.ChestIds.Contains(chest.Item.Id))
                transaction.ChestIds.Add(chest.Item.Id);
            touched.Add(chest);
        }

        if (left > 0)
        {
            await FailContractAsync(user, contractFurni, WiredTransactionFailure.FundsNoLongerAvailable);
            return;
        }

        Earn(db, user.Player.Id, (byte)terms.Contract.RewardCategory, credits, afterCommit);
        await db.SaveChangesAsync();
        foreach (var step in afterCommit)
            await step();
        await PruneChestLogsAsync(db);
        if (given.Count > 0)
            await GrainFactory.GetGrain<IPlayerGrain>(user.Player.Id).AnnounceItemsAsync(given.Select(i => i.Id).ToList());

        var rule = ContractWire.Rule(new ContractRule
        {
            Nodes = youGet.Nodes.Select(n => new ContractNode
            {
                Type = n.Type, Amount = n.Amount * terms.MaxTimes, IsWallItem = n.IsWallItem, TypeId = n.TypeId, PosterId = n.PosterId,
            }).ToList(),
        });
        SendTo(user.Player.Id, new WiredTransactionSuccessMessageComposer(WiredTransactionSuccessMessageComposer.Rewarded, rule,
            terms.Contract.RewardText, terms.Contract.ShowDialog));

        foreach (var chest in touched)
        {
            await RefreshChestLookAsync(chest);
            if (ChestSettings.Read(chest.Data).NotifyWiredTransaction)
                await NotifyChestOwnerAsync(chest, ChestWiredTransactionNotification, null);
        }

        if (_wired is not null)
            await _wired.TransactionCompletedAsync(user, contractFurni);
    }

    /// <summary>Calls off the users' transactions: under the contracts given, or any.</summary>
    private async Task CancelContractTradesAsync(RoomEntityStore.RoomEntity user, IReadOnlySet<int>? contractIds)
    {
        if (_wiredTrades.TryGetValue(user.Player.Id, out var trade) && trade.Terms is { } terms
            && (contractIds is null || contractIds.Contains(terms.ContractItemId)))
            await EndContractTradeAsync(trade, WiredTransactionFailure.TradeCancelled);
    }

    /// <summary>Ends a trade under a contract: the user is told why, and the contract's triggers hear it.</summary>
    private async Task EndContractTradeAsync(WiredTrade trade, int failure)
    {
        _wiredTrades.Remove(trade.PlayerId);
        SendTo(trade.PlayerId, new WiredTradeCancelledMessageComposer(failure));
        if (_entities?.Find(trade.PlayerId) is { } user && _items?.Find(trade.Terms!.ContractItemId) is { } contractFurni && _wired is not null)
            await _wired.TransactionFailedAsync(user, contractFurni, failure);
    }

    /// <summary>A transaction that could not start: the user is told, and the contract's triggers hear it.</summary>
    private async Task FailContractAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem contractFurni, int failure)
    {
        SendTo(user.Player.Id, new WiredTransactionFailMessageComposer(failure));
        if (_wired is not null)
            await _wired.TransactionFailedAsync(user, contractFurni, failure);
    }

    /// <summary>The contract's chests that wired may use, in the order they were given.</summary>
    private List<RoomItemStore.PlacedItem> UsableChests(IReadOnlyList<int> chestIds) =>
        chestIds.Select(Chest).OfType<RoomItemStore.PlacedItem>()
            .Where(c => ChestSettings.Read(c.Data) is { WiredEnabled: true, Locked: false }).ToList();

    private static ContractOffer OfferOf(IReadOnlyDictionary<int, int> credits, IReadOnlyDictionary<int, ChestKind> kinds) =>
        new(credits.Values.Sum(), kinds.Values.GroupBy(k => k).ToDictionary(g => g.Key, g => g.Count()));

    /// <summary>What the log keeps for a contract transaction: the contract, and the multiplier used.</summary>
    private static string ContractInfo(ContractTerms terms, int times) =>
        string.Create(CultureInfo.InvariantCulture, $"{terms.ContractItemId} {times}");

    /// <summary>The chest notification for a payment that went past what the chests could hold.</summary>
    private const string ChestEarningsNotification = "wired_chests.earnings";
}

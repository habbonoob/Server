using System.Globalization;
using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Chests;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests.Storage;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Transactions;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Chests;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Chests: furniture that stores furniture, or credits. Its owner decides who may look inside and who may
/// give to it; a Wired chest also lets those who may change the room's wired take from it, unless it is locked.
/// Whoever looks inside sees its contents change as they change. The chest's settings are its map data.</summary>
public sealed partial class RoomGrain
{
    /// <summary>The upgrade results the client has texts for ("wiredchests.upgrade.result.error.N").</summary>
    private const int UpgradeSucceeded = 0;
    private const int UpgradeAtMaximum = 2;
    private const int UpgradeSafetyLocked = 3;
    private const int UpgradeInsufficientCredits = 5;
    private const int UpgradeInsufficientDiamonds = 6;
    private const int UpgradeNotOwner = 7;
    private const int UpgradeStarterChest = 10;

    /// <summary>The credits at which the credit chest looks fuller, as its window shades it.</summary>
    private static readonly int[] CoinFillSteps = [1, 20, 100];

    /// <summary>A furni chest's open state; a credit chest's open states follow, one per fill step.</summary>
    private const int OpenChestState = 1;

    /// <summary>How the client reads a previewed item: its kind, "wall,class[,poster]"; the kinds in order, split by ';'.</summary>
    private const char PreviewSeparator = ';';

    /// <summary>The poster, the one wall item whose kind is told apart by its state.</summary>
    private const string PosterClassName = "poster";

    private readonly Dictionary<int, HashSet<Guid>> _chestViewers = [];

    /// <summary>Using a chest opens it for whoever may look inside; false for anything else.</summary>
    private bool OpenChestOnUse(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item)
    {
        if (item.Interaction is not ChestInteraction)
            return false;

        if (MayOpenChest(user, item))
            SendTo(user.Player.Id, new OpenChestMessageComposer(item.Item.Id));
        return true;
    }

    public async Task OpenChestAsync(Guid playerId, int chestId)
    {
        if (_entities?.Find(playerId) is not { } user || Chest(chestId) is not { } chest || !MayOpenChest(user, chest))
            return;

        _chestViewers.TryAdd(chestId, []);
        _chestViewers[chestId].Add(playerId);

        await using var db = await dbFactory.CreateDbContextAsync();
        if (IsCoinChest(chest))
            SendTo(playerId, new CoinsChestContentsMessageComposer(chestId, await CoinsInAsync(db, chestId), false));
        else
        {
            var storages = await StoragesAsync(db, db.UserItems.Where(i => i.ChestId == chestId).OrderBy(i => i.StoredAt));
            var size = Math.Max(1, chestOptions.Value.ContentsFragmentSize);
            var fragments = Math.Max(1, (storages.Count + size - 1) / size);
            for (var fragment = 0; fragment < fragments; fragment++)
                SendTo(playerId, new ItemsChestContentsChunkMessageComposer(chestId, fragments, fragment, storages.Skip(fragment * size).Take(size).ToList()));
        }

        await RefreshChestLookAsync(chest);
    }

    public async Task CloseChestAsync(Guid playerId, int chestId)
    {
        if (_chestViewers.TryGetValue(chestId, out var viewers) && viewers.Remove(playerId) && Chest(chestId) is { } chest)
            await RefreshChestLookAsync(chest);
    }

    public async Task WithdrawChestItemsAsync(Guid playerId, int chestId, bool isWallItem, int typeId, string posterId, int amount)
    {
        if (_entities?.Find(playerId) is not { } user || Chest(chestId) is not { } chest || IsCoinChest(chest) || !MayWithdrawFromChest(user, chest) || amount <= 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var kind = await db.UserItems.AsNoTracking().Include(i => i.Definition)
            .Where(i => i.ChestId == chestId && i.Definition!.ClassId == typeId && (i.Definition.Type == ItemType.Wall) == isWallItem)
            .OrderBy(i => i.StoredAt).ToListAsync();
        var data = await furniData.LoadAsync(db, kind);
        var taken = kind.Where(i => PosterOf(i, data[i.Id]) == posterId).Take(amount).Select(i => i.Id).ToList();
        await TakeFromChestAsync(db, user, chest, taken, 0);
    }

    public async Task WithdrawChestCoinsAsync(Guid playerId, int chestId, int amount)
    {
        if (_entities?.Find(playerId) is not { } user || Chest(chestId) is not { } chest || !IsCoinChest(chest) || !MayWithdrawFromChest(user, chest) || amount <= 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        await TakeFromChestAsync(db, user, chest, [], amount);
    }

    public async Task WithdrawAllFromChestAsync(Guid playerId, int chestId)
    {
        if (_entities?.Find(playerId) is not { } user || Chest(chestId) is not { } chest || !MayWithdrawFromChest(user, chest))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (IsCoinChest(chest))
            await TakeFromChestAsync(db, user, chest, [], await CoinsInAsync(db, chestId));
        else
            await TakeFromChestAsync(db, user, chest, await db.UserItems.Where(i => i.ChestId == chestId).Select(i => i.Id).ToListAsync(), 0);
    }

    /// <summary>Moves items and credits out of the chest to the player: the items into their inventory, the credits into their purse.</summary>
    private async Task TakeFromChestAsync(AppDbContext db, RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem chest, List<int> itemIds, int coins)
    {
        var chestId = chest.Item.Id;
        var coinRow = coins > 0 ? await db.ChestCoins.FirstOrDefaultAsync(c => c.ItemId == chestId) : null;
        if (coinRow is not null)
            coins = Math.Min(coins, coinRow.Coins);
        else
            coins = 0;
        if (itemIds.Count == 0 && coins == 0)
            return;

        var items = await db.UserItems.Include(i => i.Definition).Where(i => itemIds.Contains(i.Id) && i.ChestId == chestId).ToListAsync();
        var transaction = NewChestTransaction(ChestTransaction.Manual, string.Empty, user.Player.Id, [chestId]);
        transaction.WithdrawnFurni = items.Count;
        transaction.WithdrawnCoins = coins;
        await AddKindsAsync(db, transaction, items, deposited: false);
        db.ChestTransactions.Add(transaction);

        foreach (var item in items)
        {
            item.ChestId = null;
            item.StoredAt = null;
            item.StoredInTransactionId = null;
            item.ChestDrawOrder = null;
            item.OwnerId = user.Player.Id;
        }

        if (coinRow is not null)
            coinRow.Coins -= coins;
        await db.SaveChangesAsync();
        await PruneChestLogsAsync(db);
        var hold = HoldOf(chestId);
        hold.Items.RemoveAll(held => items.Any(i => i.Id == held.ItemId));
        hold.Coins -= coins;

        if (items.Count > 0)
        {
            await GrainFactory.GetGrain<IPlayerGrain>(user.Player.Id).AnnounceItemsAsync(items.Select(i => i.Id).ToList());
            BroadcastToChestViewers(chestId, new ItemsChestContentsUpdatedMessageComposer(chestId, items.Select(i => i.Id).ToList(), []));
        }

        if (coins > 0)
        {
            // the credits have left the chest; a payment that fails is logged with what it owed
            try
            {
                var balance = await GrainFactory.GetGrain<IPlayerGrain>(user.Player.Id).AddCreditsAsync(coins, CurrencyTransactionReasons.ChestWithdrawal);
                SendTo(user.Player.Id, new CreditBalanceComposer(balance));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Paying player {Player} {Credits} credits taken from chest {Chest} failed", user.Player.Id, coins, chestId);
            }

            BroadcastToChestViewers(chestId, new CoinsChestContentsMessageComposer(chestId, coinRow!.Coins, true));
        }

        SendTo(user.Player.Id, WiredTransactionSuccessMessageComposer.Of(WiredTransactionSuccessMessageComposer.Withdrawn));
        await RefreshChestLookAsync(chest);

        var settings = ChestSettings.Read(chest.Data);
        if (settings.WiredEnabled && settings.NotifySomeoneWithdraws && user.Player.Id != chest.Item.OwnerId)
            await NotifyChestOwnerAsync(chest, SomeoneWithdrawsNotification, user.Player.Username);
        if (settings.WiredEnabled && settings.NotifyChestEmpty && await HeldByAsync(db, chest) == 0)
            await NotifyChestOwnerAsync(chest, ChestEmptyNotification, null);
    }

    public async Task SetChestOptionsAsync(Guid playerId, int chestId, bool locked, bool autoLock, int capacity)
    {
        if (_entities?.Find(playerId) is not { } user || Chest(chestId) is not { } chest)
            return;

        var settings = ChestSettings.Read(chest.Data);
        var isOwner = chest.Item.OwnerId == playerId;

        // the owner sets everything; the room's owner may only lock it
        if (!isOwner)
        {
            if (locked && !settings.Locked && _room!.OwnerId == playerId)
                await SaveChestSettingsAsync(chest, settings with { Locked = true });
            return;
        }

        var max = await MaxChestCapacityAsync(chest, settings.CapacityLevel);
        await SaveChestSettingsAsync(chest, settings with { Locked = locked, AutoLock = autoLock, Capacity = Math.Clamp(capacity, 0, max) });
    }

    public async Task SetChestPreferencesAsync(Guid playerId, int chestId, string name, string description, bool everyoneCanOpen,
        bool everyoneCanDonate, int stateControlMode, int previewMode, int previewAmount, bool wiredEnabled)
    {
        if (Chest(chestId) is not { } chest || chest.Item.OwnerId != playerId)
            return;

        var settings = ChestSettings.Read(chest.Data);
        var starter = (await chestRules.GetAsync()).IsStarter(chest.Item.Definition!.ClassName);
        var filteredName = (await wordFilter.FilterAsync(Truncate(name, ChestSettings.NameMaxLength), _filterWords)).Text;
        var filteredDescription = (await wordFilter.FilterAsync(Truncate(description, ChestSettings.DescriptionMaxLength), _filterWords)).Text;

        // becoming a Wired chest cannot be undone, and a starter chest never becomes one
        await SaveChestSettingsAsync(chest, settings with
        {
            Name = filteredName, Description = filteredDescription, EveryoneCanOpen = everyoneCanOpen, EveryoneCanDonate = everyoneCanDonate,
            StateControlMode = Math.Clamp(stateControlMode, ChestSettings.OpenWhenLookedInto, ChestSettings.ControlledByWired),
            PreviewMode = Math.Clamp(previewMode, ChestSettings.PreviewNone, ChestSettings.PreviewNextForWired),
            PreviewAmount = Math.Clamp(previewAmount, ChestSettings.MinPreviewAmount, ChestSettings.MaxPreviewAmount),
            WiredEnabled = settings.WiredEnabled || (wiredEnabled && !starter),
        });
        SendTo(playerId, new ChestPreferencesUpdateSuccessMessageComposer(chestId, false));
        await RefreshChestLookAsync(chest);
    }

    public async Task SetChestNotificationsAsync(Guid playerId, int chestId, int notifyMode, bool chestFull, bool donation,
        bool someoneWithdraws, bool chestEmpty, bool wiredTransaction)
    {
        if (Chest(chestId) is not { } chest || chest.Item.OwnerId != playerId)
            return;

        await SaveChestSettingsAsync(chest, ChestSettings.Read(chest.Data) with
        {
            NotifyMode = Math.Clamp(notifyMode, ChestSettings.NotifyAlways, ChestSettings.NotifyWhenAway), NotifyChestFull = chestFull,
            NotifyDonation = donation, NotifySomeoneWithdraws = someoneWithdraws, NotifyChestEmpty = chestEmpty, NotifyWiredTransaction = wiredTransaction,
        });
        SendTo(playerId, new ChestPreferencesUpdateSuccessMessageComposer(chestId, true));
    }

    /// <summary>Locks or unlocks the player's own chests here; the room's owner, or staff, may lock every chest in it.</summary>
    public async Task LockAllChestsAsync(Guid playerId, bool locked, bool allChests)
    {
        if (_entities?.Find(playerId) is not { } user || _items is null)
            return;
        if (allChests ? !locked || (_room!.OwnerId != playerId && !user.Player.IsModerator) : !WiredRights(user).Modify)
            return;

        foreach (var chest in _items.Items.Where(i => i.Interaction is ChestInteraction && (allChests || i.Item.OwnerId == playerId)).ToList())
        {
            var settings = ChestSettings.Read(chest.Data);
            if (settings.Locked != locked)
                await SaveChestSettingsAsync(chest, settings with { Locked = locked });
        }
    }

    public async Task UpgradeChestAsync(Guid playerId, int chestId, int levels)
    {
        if (_entities?.Find(playerId) is not { } user || Chest(chestId) is not { } chest || levels <= 0)
            return;

        var result = await TryUpgradeChestAsync(user, chest, levels);
        SendTo(playerId, new UpgradeChestResultMessageComposer(chestId, result));
    }

    private async Task<int> TryUpgradeChestAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem chest, int levels)
    {
        var limits = await chestRules.GetAsync();
        var settings = ChestSettings.Read(chest.Data);
        var coins = IsCoinChest(chest);
        if (chest.Item.OwnerId != user.Player.Id)
            return UpgradeNotOwner;
        if (limits.IsStarter(chest.Item.Definition!.ClassName))
            return UpgradeStarterChest;
        // the client's count is compared, not added: a huge one would overflow the sum past the maximum, and the price below zero
        if (levels > limits.MaxUpgrades(coins) - settings.CapacityLevel)
            return UpgradeAtMaximum;
        if (user.Player.AccountSafetyLocked)
            return UpgradeSafetyLocked;

        var player = GrainFactory.GetGrain<IPlayerGrain>(user.Player.Id);
        var credits = limits.UpgradeCostCredits * levels;
        var diamonds = limits.UpgradeCostDiamonds * levels;
        if (await player.GetActivityPointsAsync(ChestLimits.DiamondsType) < diamonds)
            return UpgradeInsufficientDiamonds;

        int? balance = null;
        if (credits > 0 && (balance = await player.SpendCreditsAsync(credits, CurrencyTransactionReasons.ChestUpgrade)) is null)
            return UpgradeInsufficientCredits;
        if (diamonds > 0 && !await player.SpendActivityPointsAsync(ChestLimits.DiamondsType, diamonds, CurrencyTransactionReasons.ChestUpgrade))
        {
            // the credits are given back: nothing was bought
            if (balance is not null)
                SendTo(user.Player.Id, new CreditBalanceComposer(await player.AddCreditsAsync(credits, CurrencyTransactionReasons.ChestUpgrade)));
            return UpgradeInsufficientDiamonds;
        }

        // a chest filled to what it could hold grows with what was bought
        var oldMax = limits.MaxCapacity(coins, false, settings.CapacityLevel);
        var newLevel = settings.CapacityLevel + levels;
        var capacity = settings.Capacity is { } set && set < oldMax ? set : limits.MaxCapacity(coins, false, newLevel);
        await SaveChestSettingsAsync(chest, settings with { CapacityLevel = newLevel, Capacity = capacity });
        return UpgradeSucceeded;
    }

    /// <summary>A placed chest starts locked, and takes the capacity its kind allows if it has none yet.</summary>
    private async Task LockPlacedChestAsync(RoomItemStore.PlacedItem placed)
    {
        if (placed.Interaction is not ChestInteraction)
            return;

        var settings = ChestSettings.Read(placed.Data);
        await SaveChestSettingsAsync(placed, settings with
        {
            Locked = true, Capacity = settings.Capacity ?? await MaxChestCapacityAsync(placed, settings.CapacityLevel),
        });

        // what it holds came with it
        await using var db = await dbFactory.CreateDbContextAsync();
        await LoadChestHoldAsync(db, placed.Item.Id);
        var placedSettings = ChestSettings.Read(placed.Data);
        if (ContentsCountOf(placed) is var count && count != placedSettings.ContentsCount)
            await SaveChestSettingsAsync(placed, placedSettings with { ContentsCount = count });
    }

    /// <summary>The owner leaving locks their chests set to lock then, and stops them looking into any.</summary>
    private async Task LeaveChestsAsync(Guid playerId)
    {
        foreach (var (chestId, viewers) in _chestViewers.ToList())
        {
            if (viewers.Remove(playerId) && Chest(chestId) is { } viewed)
                await RefreshChestLookAsync(viewed);
        }

        if (_items is null)
            return;

        foreach (var chest in _items.Items.Where(i => i.Interaction is ChestInteraction && i.Item.OwnerId == playerId).ToList())
        {
            var settings = ChestSettings.Read(chest.Data);
            if (settings.AutoLock && !settings.Locked)
                await SaveChestSettingsAsync(chest, settings with { Locked = true });
        }
    }

    private RoomItemStore.PlacedItem? Chest(int chestId) => _items?.Find(chestId) is { Interaction: ChestInteraction } chest ? chest : null;

    private static bool IsCoinChest(RoomItemStore.PlacedItem chest) => chest.Interaction is ChestInteraction { HoldsCoins: true };

    /// <summary>The owner, anyone when the chest is open to everyone, and those who may read the room's wired.</summary>
    private bool MayOpenChest(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem chest) =>
        chest.Item.OwnerId == user.Player.Id || ChestSettings.Read(chest.Data).EveryoneCanOpen || WiredRights(user).Read;

    /// <summary>The owner, anyone when the chest takes donations, and on an unlocked Wired chest those who may change the room's wired.</summary>
    private bool MayDepositIntoChest(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem chest)
    {
        var settings = ChestSettings.Read(chest.Data);
        return chest.Item.OwnerId == user.Player.Id || settings.EveryoneCanDonate || (settings.WiredEnabled && !settings.Locked && WiredRights(user).Modify);
    }

    /// <summary>The owner, and on an unlocked Wired chest those who may change the room's wired.</summary>
    private bool MayWithdrawFromChest(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem chest)
    {
        var settings = ChestSettings.Read(chest.Data);
        return chest.Item.OwnerId == user.Player.Id || (settings.WiredEnabled && !settings.Locked && WiredRights(user).Modify);
    }

    /// <summary>What the chest may hold: its capacity, which the owner may set below the most its kind allows.</summary>
    private async Task<int> ChestCapacityAsync(RoomItemStore.PlacedItem chest)
    {
        var settings = ChestSettings.Read(chest.Data);
        var max = await MaxChestCapacityAsync(chest, settings.CapacityLevel);
        return Math.Min(settings.Capacity ?? max, max);
    }

    private async Task<int> MaxChestCapacityAsync(RoomItemStore.PlacedItem chest, int level)
    {
        var limits = await chestRules.GetAsync();
        return limits.MaxCapacity(IsCoinChest(chest), limits.IsStarter(chest.Item.Definition!.ClassName), level);
    }

    /// <summary>How much the chest holds: its items, or its credits.</summary>
    private async Task<int> HeldByAsync(AppDbContext db, RoomItemStore.PlacedItem chest) =>
        IsCoinChest(chest) ? await CoinsInAsync(db, chest.Item.Id) : await db.UserItems.CountAsync(i => i.ChestId == chest.Item.Id);

    private static async Task<int> CoinsInAsync(AppDbContext db, int chestId) =>
        await db.ChestCoins.Where(c => c.ItemId == chestId).Select(c => c.Coins).FirstOrDefaultAsync();

    private async Task SaveChestSettingsAsync(RoomItemStore.PlacedItem chest, ChestSettings settings) =>
        await _furniture!.UpdateAsync(chest, settings.ToData());

    /// <summary>Opens or closes the chest as its settings say, and shows the items it previews on top while open.</summary>
    private async Task RefreshChestLookAsync(RoomItemStore.PlacedItem chest)
    {
        var settings = ChestSettings.Read(chest.Data);
        var viewed = _chestViewers.TryGetValue(chest.Item.Id, out var viewers) && viewers.Count > 0;
        var open = settings.StateControlMode switch
        {
            ChestSettings.AlwaysOpen => true,
            ChestSettings.AlwaysClosed => false,
            ChestSettings.ControlledByWired => settings.State != ChestSettings.ClosedState,
            _ => viewed,
        };

        await using var db = await dbFactory.CreateDbContextAsync();
        int state;
        var visuals = string.Empty;
        if (IsCoinChest(chest))
        {
            var coins = await CoinsInAsync(db, chest.Item.Id);
            state = open ? OpenChestState + CoinFillSteps.Count(step => coins >= step) : ChestSettings.ClosedState;
        }
        else
        {
            state = open ? OpenChestState : ChestSettings.ClosedState;
            if (open && settings.PreviewMode != ChestSettings.PreviewNone)
                visuals = await PreviewAsync(db, chest.Item.Id, settings.PreviewMode, settings.PreviewAmount);
        }

        var count = ContentsCountOf(chest);
        if (state != settings.State || visuals != settings.Visuals || count != settings.ContentsCount)
            await SaveChestSettingsAsync(chest, settings with { State = state, Visuals = visuals, ContentsCount = count });
    }

    /// <summary>What the chest holds, as the client counts it (contents_count): credits in a coin chest, items in any other.</summary>
    private int ContentsCountOf(RoomItemStore.PlacedItem chest)
    {
        var hold = HoldOf(chest.Item.Id);
        return IsCoinChest(chest) ? hold.Coins : hold.Items.Count;
    }

    /// <summary>Every chest here shows the count of what it holds: those saved before chests kept one, or changed while
    /// the room was not loaded.</summary>
    private async Task SyncChestCountsAsync()
    {
        foreach (var chest in _items!.Items.Where(i => i.Interaction is ChestInteraction).ToList())
        {
            var settings = ChestSettings.Read(chest.Data);
            var count = ContentsCountOf(chest);
            if (count != settings.ContentsCount)
                await SaveChestSettingsAsync(chest, settings with { ContentsCount = count });
        }
    }

    /// <summary>The kinds of the items the open chest shows, chosen as its preview mode says.</summary>
    private async Task<string> PreviewAsync(AppDbContext db, int chestId, int mode, int amount)
    {
        var held = db.UserItems.AsNoTracking().Include(i => i.Definition).Where(i => i.ChestId == chestId);
        List<UserItem> candidates = mode switch
        {
            ChestSettings.PreviewNewest or ChestSettings.PreviewNewestKinds => await held.OrderByDescending(i => i.StoredAt).ToListAsync(),
            ChestSettings.PreviewOldest or ChestSettings.PreviewOldestKinds => await held.OrderBy(i => i.StoredAt).ToListAsync(),
            ChestSettings.PreviewNextForWired => await held.OrderBy(i => i.ChestDrawOrder).ToListAsync(),
            _ => (await held.ToListAsync()).OrderBy(_ => _chestRandom.Next()).ToList(),
        };
        var data = await furniData.LoadAsync(db, candidates);
        var kinds = candidates.Select(i => PreviewKind(i, data[i.Id])).ToList();

        // the starred modes show different kinds first, and only repeat one when there are too few
        var preferKinds = mode is ChestSettings.PreviewRandomKinds or ChestSettings.PreviewNewestKinds or ChestSettings.PreviewOldestKinds;
        var shown = preferKinds ? kinds.Distinct().Concat(kinds).Take(amount) : kinds.Take(amount);
        return string.Join(PreviewSeparator, shown);
    }

    private readonly Random _chestRandom = new();

    private static string PreviewKind(UserItem item, FurniDataView data)
    {
        var isWall = item.Definition!.Type == ItemType.Wall ? "true" : "false";
        var poster = PosterOf(item, data);
        return poster.Length > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{isWall},{item.Definition.ClassId},{poster}")
            : string.Create(CultureInfo.InvariantCulture, $"{isWall},{item.Definition.ClassId}");
    }

    private static string PosterOf(UserItem item, FurniDataView data) =>
        item.Definition!.ClassName == PosterClassName ? data.StateText : string.Empty;

    /// <summary>The chest's items as its window lists them.</summary>
    private async Task<List<ChestStorageData>> StoragesAsync(AppDbContext db, IQueryable<UserItem> query)
    {
        var items = await query.AsNoTracking().Include(i => i.Definition).ToListAsync();
        var data = await furniData.LoadAsync(db, items);
        return items.Select(i => ToStorage(i, data[i.Id])).ToList();
    }

    private static ChestStorageData ToStorage(UserItem item, FurniDataView data)
    {
        var isWall = item.Definition!.Type == ItemType.Wall;
        return new ChestStorageData(item.Id, ChestStorageData.Unlocked, item.StoredInTransactionId ?? 0,
            new ChestItemTypeData(isWall, item.Definition.ClassId, PosterOf(item, data)), item.Definition.Groupable && data.IsPlain,
            FurniCategory.Of(item.Definition), FurniStuffData.From(data), isWall ? null : item.Extra);
    }

    private void BroadcastToChestViewers(int chestId, Habbo.Protocol.Composers.IComposer composer)
    {
        if (!_chestViewers.TryGetValue(chestId, out var viewers))
            return;

        foreach (var viewer in viewers)
            SendTo(viewer, composer);
    }

    private ChestTransaction NewChestTransaction(int type, string info, Guid? userId, List<int> chestIds) =>
        new() { RoomId = _room!.Id, Type = type, Info = info, UserId = userId, ChestIds = chestIds };

    /// <summary>Counts the items by kind onto the transaction.</summary>
    private async Task AddKindsAsync(AppDbContext db, ChestTransaction transaction, List<UserItem> items, bool deposited)
    {
        var data = await furniData.LoadAsync(db, items);
        foreach (var kind in items.GroupBy(i => (IsWall: i.Definition!.Type == ItemType.Wall, i.Definition.ClassId, Poster: PosterOf(i, data[i.Id]))))
        {
            transaction.Items.Add(new ChestTransactionItem
            {
                Deposited = deposited, IsWallItem = kind.Key.IsWall, TypeId = kind.Key.ClassId, PosterId = kind.Key.Poster, Amount = kind.Count(),
            });
        }
    }

    /// <summary>The chest notifications the client has settings for ("notification.wired_chests.*").</summary>
    private const string ChestFullNotification = "wired_chests.chest_full";
    private const string DonationNotification = "wired_chests.donation";
    private const string SomeoneWithdrawsNotification = "wired_chests.someone_withdraws";
    private const string ChestEmptyNotification = "wired_chests.chest_empty";

    /// <summary>Tells the chest's owner, unless they only want to hear while away and are in the room.</summary>
    private async Task NotifyChestOwnerAsync(RoomItemStore.PlacedItem chest, string type, string? userName)
    {
        var settings = ChestSettings.Read(chest.Data);
        if (settings.NotifyMode == ChestSettings.NotifyWhenAway && _entities?.Find(chest.Item.OwnerId) is not null)
            return;

        var name = settings.Name.Length > 0 ? settings.Name : chest.Item.Definition!.Name;
        var parameters = new Dictionary<string, string>
        {
            ["chest_name"] = name,
            ["linkUrl"] = string.Create(CultureInfo.InvariantCulture, $"event:navigator/goto/{_room!.Id}"),
        };
        if (userName is not null)
            parameters["user_name"] = userName;
        await deferredNotifications.NotifyAsync(chest.Item.OwnerId, type, parameters);
    }

    private static string Truncate(string text, int length) => text.Length > length ? text[..length] : text;
}

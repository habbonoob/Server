using Habbo.Domain.Furniture;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Protocol.Messages.Outgoing.Inventory.Trading;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Trading between two avatars in the room, where the room allows it. What is offered stays in
/// each player's inventory until both have accepted and then confirmed; then it changes hands in one
/// go, and credit furni offered is paid to the receiver in credits. Leaving the room ends a trade.</summary>
public sealed partial class RoomGrain
{
    /// <summary>What a player may trade: their own items in their inventory, not on the marketplace, not in a chest
    /// nor a chest holding anything (items or credits), and tradeable.</summary>
    private static IQueryable<UserItem> TradeableItems(AppDbContext db, Guid ownerId) =>
        db.UserItems.Where(i => i.OwnerId == ownerId && i.RoomId == null && !i.InMarketplace && i.ChestId == null
                                && !db.UserItems.Any(stored => stored.ChestId == i.Id) && !db.ChestCoins.Any(c => c.ItemId == i.Id && c.Coins > 0) && i.Tradeable && i.Definition!.Tradeable);

    /// <summary>The room's trade settings, as the room settings dialog numbers them.</summary>
    private const int TradeNotAllowed = 0;
    private const int TradeControllersOnly = 1;

    private const int May = 1;
    private const int MayNot = 0;
    private const string CreditsFromTrade = "trade";

    private readonly Dictionary<Guid, RoomTrade> _trades = [];

    public async Task OpenTradingAsync(Guid playerId, int roomIndex)
    {
        if (_room is null || _entities?.Find(playerId) is not { } me || _entities.FindByIndex(roomIndex) is not { } other || other == me)
            return;

        var failure = !tradingOptions.Value.Enabled ? TradeOpenFailedComposer.HotelDisabled
            : _room.TradeMode == TradeNotAllowed || (_room.TradeMode == TradeControllersOnly && !IsController(playerId)) ? TradeOpenFailedComposer.RoomDisallows
            : me.Player.TradeLocked ? TradeOpenFailedComposer.YouAreTradeLocked
            : other.Player.TradeLocked ? TradeOpenFailedComposer.OtherCannotTrade
            : _trades.ContainsKey(me.Player.Id) || _wiredTrades.ContainsKey(me.Player.Id) ? TradeOpenFailedComposer.YouAreTrading
            : _trades.ContainsKey(other.Player.Id) ? TradeOpenFailedComposer.OtherIsTrading
            : (int?)null;
        if (failure is { } reason)
        {
            SendTo(playerId, new TradeOpenFailedComposer(reason, other.Player.Username));
            return;
        }

        // an account without trading in use only receives, and both are told
        var mayGive = await MayGiveInTradeAsync([me.Player.Id, other.Player.Id]);
        var trade = new RoomTrade(new TradeSide(me, mayGive.Contains(me.Player.Id)), new TradeSide(other, mayGive.Contains(other.Player.Id)));
        _trades[me.Player.Id] = trade;
        _trades[other.Player.Id] = trade;
        me.Trading = other.Trading = true;
        Broadcast(UserUpdateMessageComposer.From([_entities.CurrentStatus(me), _entities.CurrentStatus(other)]));

        var opened = new TradingOpenComposer(me.Player.AccountId, trade.First.MayGive ? May : MayNot, other.Player.AccountId, trade.Second.MayGive ? May : MayNot);
        SendTo(me.Player.Id, opened);
        SendTo(other.Player.Id, opened);
        foreach (var side in trade.Sides.Where(s => !s.MayGive))
        {
            SendTo(side.Entity.Player.Id, new TradingYouAreNotAllowedComposer());
            SendTo(trade.OtherThan(side.Entity.Player.Id).Entity.Player.Id, new TradingOtherNotAllowedComposer());
        }
    }

    /// <summary>Which of the players may give in a trade: all of them, unless giving needs an activated email.</summary>
    private async Task<HashSet<Guid>> MayGiveInTradeAsync(IReadOnlyList<Guid> playerIds)
    {
        if (!tradingOptions.Value.RequireVerifiedEmail)
            return playerIds.ToHashSet();

        await using var db = await dbFactory.CreateDbContextAsync();
        return (await db.Users.Where(u => playerIds.Contains(u.Id) && u.EmailVerified).Select(u => u.Id).ToListAsync()).ToHashSet();
    }

    public async Task AddItemsToTradeAsync(Guid playerId, IReadOnlyList<int> itemIds)
    {
        if (!_trades.TryGetValue(playerId, out var trade))
        {
            SendTo(playerId, new TradingNotOpenComposer());
            return;
        }

        if (trade.Confirming)
            return;

        var side = trade.SideOf(playerId);
        if (!side.MayGive)
        {
            SendTo(playerId, new TradingYouAreNotAllowedComposer());
            return;
        }

        var wanted = itemIds.Distinct().Where(id => !side.Items.ContainsKey(id))
            .Take(tradingOptions.Value.ItemLimit - side.Items.Count).ToList();
        if (wanted.Count == 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var items = await TradeableItems(db, playerId).AsNoTracking().Include(i => i.Definition)
            .Where(i => wanted.Contains(i.Id))
            .ToListAsync();
        if (items.Count == 0)
        {
            SendTo(playerId, new TradingNoSuchItemComposer());
            return;
        }

        var data = await furniData.LoadAsync(db, items);
        foreach (var item in items)
        {
            side.Items[item.Id] = ToTradeItem(item, data[item.Id]);
            if (item.Definition!.CreditValue is { } credits and > 0)
                side.CreditValues[item.Id] = credits;
        }

        TradeChanged(trade);
    }

    public Task RemoveItemFromTradeAsync(Guid playerId, int itemId)
    {
        if (_trades.TryGetValue(playerId, out var trade) && !trade.Confirming)
        {
            var side = trade.SideOf(playerId);
            if (side.Items.Remove(itemId))
            {
                side.CreditValues.Remove(itemId);
                TradeChanged(trade);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>What is on the table changed: the acceptances are taken back, and both see it.</summary>
    private void TradeChanged(RoomTrade trade)
    {
        foreach (var side in trade.Sides)
        {
            if (side.Accepted)
            {
                side.Accepted = false;
                SendToTrade(trade, new TradingAcceptComposer(side.Entity.Player.AccountId, MayNot));
            }
        }

        SendToTrade(trade, new TradingItemListComposer(
            trade.First.Entity.Player.AccountId, trade.First.Items.Values.ToList(), trade.First.Items.Count, trade.First.Credits,
            trade.Second.Entity.Player.AccountId, trade.Second.Items.Values.ToList(), trade.Second.Items.Count, trade.Second.Credits));
    }

    public Task SetTradeAcceptedAsync(Guid playerId, bool accepted)
    {
        if (!_trades.TryGetValue(playerId, out var trade) || trade.Confirming)
            return Task.CompletedTask;

        var side = trade.SideOf(playerId);
        side.Accepted = accepted;
        SendToTrade(trade, new TradingAcceptComposer(side.Entity.Player.AccountId, accepted ? May : MayNot));
        if (trade.First.Accepted && trade.Second.Accepted)
        {
            trade.Confirming = true;
            SendToTrade(trade, new TradingConfirmationComposer());
        }

        return Task.CompletedTask;
    }

    public async Task ConfirmTradeAsync(Guid playerId)
    {
        if (!_trades.TryGetValue(playerId, out var trade) || !trade.Confirming)
            return;

        trade.SideOf(playerId).Confirmed = true;
        if (trade.First.Confirmed && trade.Second.Confirmed)
            await CommitTradeAsync(trade);
    }

    public Task CloseTradeAsync(Guid playerId)
    {
        if (_trades.TryGetValue(playerId, out var trade))
            EndTrade(trade, new TradingCloseComposer(trade.SideOf(playerId).Entity.Player.AccountId, TradingCloseComposer.Closed));
        return Task.CompletedTask;
    }

    /// <summary>Hands everything over in one transaction; if anything offered is no longer where it was,
    /// nothing changes hands and the trade fails.</summary>
    private async Task CommitTradeAsync(RoomTrade trade)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        foreach (var side in trade.Sides)
        {
            var receiver = trade.OtherThan(side.Entity.Player.Id).Entity.Player.Id;
            var ids = side.Items.Keys.ToList();
            // checked again: since it was offered, an item may have been placed, listed on the marketplace or put in a chest
            var moved = await TradeableItems(db, side.Entity.Player.Id).Where(i => ids.Contains(i.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.OwnerId, receiver));
            if (moved != ids.Count)
            {
                await transaction.RollbackAsync();
                EndTrade(trade, new TradingCloseComposer(side.Entity.Player.AccountId, TradingCloseComposer.CommitFailed));
                return;
            }

            // credit furni is paid out to whoever receives it
            var creditIds = side.CreditValues.Keys.ToList();
            await db.UserItems.Where(i => creditIds.Contains(i.Id)).ExecuteDeleteAsync();
        }

        await transaction.CommitAsync();

        // the trade has happened: it ends whatever goes wrong telling anyone
        try
        {
            SendToTrade(trade, new TradingCompletedComposer());

            // both traders: a trade done, and the credit furni it moved
            var coins = trade.Sides.Sum(side => side.CreditValues.Values.Sum());
            foreach (var side in trade.Sides)
            {
                var trader = GrainFactory.GetGrain<IPlayerGrain>(side.Entity.Player.Id);
                trader.ProgressAchievementAsync(AchievementNames.TradeCount, 1).Ignore();
                if (coins > 0)
                    trader.ProgressAchievementAsync(AchievementNames.CoinValueTraded, coins).Ignore();
            }

            foreach (var side in trade.Sides)
            {
                var giver = side.Entity.Player.Id;
                var receiver = trade.OtherThan(giver).Entity.Player.Id;
                foreach (var itemId in side.Items.Keys)
                    SendTo(giver, FurniListRemoveComposer.Item(itemId));

                var receiverGrain = GrainFactory.GetGrain<IPlayerGrain>(receiver);
                if (side.Credits > 0)
                    await PayTradedCreditsAsync(receiverGrain, receiver, side.Credits);
                await receiverGrain.AnnounceItemsAsync(side.Items.Keys.Where(id => !side.CreditValues.ContainsKey(id)).ToList());
            }
        }
        finally
        {
            EndTrade(trade, null);
        }
    }

    /// <summary>Pays the receiver the credit furni they were traded, which is already gone. A payment that fails is
    /// logged with what it owed, since the furni cannot come back.</summary>
    private async Task PayTradedCreditsAsync(IPlayerGrain receiverGrain, Guid receiver, int credits)
    {
        try
        {
            SendTo(receiver, new CreditBalanceComposer(await receiverGrain.AddCreditsAsync(credits, CreditsFromTrade)));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Paying player {Player} {Credits} credits for traded credit furni failed", receiver, credits);
        }
    }

    /// <summary>Ends the trade for both, telling them why when there is something to tell.</summary>
    private void EndTrade(RoomTrade trade, TradingCloseComposer? closed)
    {
        if (closed is not null)
            SendToTrade(trade, closed);

        foreach (var side in trade.Sides)
        {
            _trades.Remove(side.Entity.Player.Id);
            side.Entity.Trading = false;
        }

        if (_entities is not null)
            Broadcast(UserUpdateMessageComposer.From(trade.Sides.Where(s => _entities.Find(s.Entity.Player.Id) is not null)
                .Select(s => _entities.CurrentStatus(s.Entity)).ToList()));
    }

    private void SendToTrade(RoomTrade trade, Habbo.Protocol.Composers.IComposer composer)
    {
        foreach (var side in trade.Sides)
            SendTo(side.Entity.Player.Id, composer);
    }

    /// <summary>An item as the trade window shows it.</summary>
    private static TradeItemData ToTradeItem(UserItem item, FurniDataView data)
    {
        var view = InventoryProjection.From(item, item.Definition!, data);
        var created = item.CreatedAt.UtcDateTime;
        var isWall = item.Definition!.Type == ItemType.Wall;
        return new TradeItemData(InventoryStripId.Of(item.Id), isWall ? TradeItemData.WallType : TradeItemData.FloorType, item.Id, item.Definition.ClassId,
            view.Category, view.Groupable, FurniStuffData.From(data), created.Day, created.Month, created.Year,
            isWall ? null : item.Extra);
    }
}

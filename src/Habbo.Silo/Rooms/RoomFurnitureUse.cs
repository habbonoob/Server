using Habbo.Domain.Furniture;
using Habbo.Domain.Rooms;
using Habbo.Infrastructure;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Items;
using Habbo.Silo.Items.Interactions;
using Microsoft.EntityFrameworkCore;
using Orleans;

namespace Habbo.Silo.Rooms;

/// <summary>Runs furniture behaviours inside one room: checks the player may use the item, hands
/// the use to the item's behaviour, and stores and shows whatever it changes. The grain supplies
/// how to broadcast and how to schedule, since both belong to it.</summary>
public sealed class RoomFurnitureUse(
    RoomItemStore items,
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    Action<IComposer> broadcast,
    Action<Guid, IComposer> sendTo,
    Action<TimeSpan, Func<Task>> schedule,
    Func<RoomEntityStore.RoomEntity, int, int, Func<Task>, Task> walkThen,
    Action<RoomEntityStore.RoomEntity, int, int> lookAt,
    Action<RoomEntityStore.RoomEntity, int> setHandItem,
    Action<RoomEntityStore.RoomEntity, int> wearEffect,
    Action<RoomEntityStore.RoomEntity, string?> dressInRoom,
    Func<int, int, bool> hasAvatarAt) : IFurniRoom
{
    public bool IsOccupied(RoomItemStore.PlacedItem item) => RoomItemStore.Footprint(item).Any(tile => hasAvatarAt(tile.X, tile.Y));

    public void Tell(RoomEntityStore.RoomEntity user, IComposer composer) => sendTo(user.Player.Id, composer);

    public Task WalkThenAsync(RoomEntityStore.RoomEntity user, int x, int y, Func<Task> then) => walkThen(user, x, y, then);

    public void LookAt(RoomEntityStore.RoomEntity user, int x, int y) => lookAt(user, x, y);

    public void SetHandItem(RoomEntityStore.RoomEntity user, int handItem) => setHandItem(user, handItem);

    public void WearEffect(RoomEntityStore.RoomEntity user, int effectId) => wearEffect(user, effectId);

    public void DressInRoom(RoomEntityStore.RoomEntity user, string? outfitFigure) => dressInRoom(user, outfitFigure);

    /// <summary>How far away, in tiles, an avatar can still touch an item.</summary>
    private const int Reach = 1;

    /// <summary>The removal messages name whoever picked the item up; an exchanged item is
    /// destroyed rather than picked up, so nobody did.</summary>
    private const int NobodyPickedItUp = 0;

    public Task UseAsync(RoomEntityStore.RoomEntity user, bool isController, int itemId, FurniAction action, int param)
    {
        if (items.Find(itemId) is not { } placed || !Permits(placed.Interaction.UsagePolicy, isController))
            return Task.CompletedTask;

        return placed.Interaction.UseAsync(new FurniUse(placed, user, action, param, this));
    }

    private static bool Permits(FurniUsagePolicy policy, bool isController) => policy switch
    {
        FurniUsagePolicy.Everyone => true,
        FurniUsagePolicy.Controllers => isController,
        _ => false,
    };

    /// <summary>Told of every state change, whatever made it: the room's wired listens.</summary>
    public Func<RoomItemStore.PlacedItem, Task>? OnStateChanged { get; set; }

    public Task UpdateAsync(RoomItemStore.PlacedItem item, FurniDataView data) => UpdateAsync(item, data, persist: true);

    /// <summary>Gives the item new data and shows the room. <paramref name="persist"/> false keeps it in the room only,
    /// for what changes every second (a running clock) and is saved when it settles.</summary>
    public async Task UpdateAsync(RoomItemStore.PlacedItem item, FurniDataView data, bool persist)
    {
        // an item no longer placed here (picked up while a delayed effect waited) keeps the data it left with
        if (items.Find(item.Item.Id) != item)
            return;

        var stateChanged = item.Data.State != data.State;
        if (persist)
            await SaveAsync(item, data);
        else
            item.Data = data;
        ShowData(item);

        if (stateChanged && OnStateChanged is { } onStateChanged)
            await onStateChanged(item);
    }

    // What changed while a batch is open, shown all at once when it closes: the latest data of each item.
    private int _batchDepth;
    private readonly Dictionary<int, FurniStuffData> _pendingObjects = [];
    private readonly Dictionary<int, string> _pendingWallStates = [];

    public void ShowData(RoomItemStore.PlacedItem item)
    {
        var id = item.Item.Id;
        if (item.Item.Definition!.Type == ItemType.Wall)
        {
            if (_batchDepth > 0)
                _pendingWallStates[id] = item.Data.StateText;
            else
                broadcast(new ItemStateUpdateMessageComposer(id, item.Data.StateText));
        }
        else if (_batchDepth > 0)
            _pendingObjects[id] = FurniStuffData.From(item.Data);
        else
            broadcast(new ObjectDataUpdateMessageComposer(id, FurniStuffData.From(item.Data)));
    }

    /// <summary>Runs work that may change many items' data, such as a wired pulse, and shows the room what changed
    /// in one message for the floor items and one for the wall items.</summary>
    public async Task BatchedAsync(Func<Task> work)
    {
        _batchDepth++;
        try
        {
            await work();
        }
        finally
        {
            if (--_batchDepth == 0)
                FlushBatch();
        }
    }

    private void FlushBatch()
    {
        if (_pendingObjects.Count == 1)
            broadcast(new ObjectDataUpdateMessageComposer(_pendingObjects.Keys.First(), _pendingObjects.Values.First()));
        else if (_pendingObjects.Count > 1)
            broadcast(new ObjectsDataUpdateMessageComposer(_pendingObjects.Select(p => new ObjectDataEntry(p.Key, p.Value)).ToList()));

        if (_pendingWallStates.Count == 1)
            broadcast(new ItemStateUpdateMessageComposer(_pendingWallStates.Keys.First(), _pendingWallStates.Values.First()));
        else if (_pendingWallStates.Count > 1)
            broadcast(new ItemsStateUpdateMessageComposer(_pendingWallStates.Select(p => new ItemStateData(p.Key, p.Value)).ToList()));

        _pendingObjects.Clear();
        _pendingWallStates.Clear();
    }

    public async Task SaveAsync(RoomItemStore.PlacedItem item, FurniDataView data)
    {
        var previous = item.Data;
        item.Data = data;
        if (item.Temporary)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        await FurniDataStore.SaveAsync(db, item.Item.Id, previous, data);
    }

    public async Task<bool> RemoveAsync(RoomItemStore.PlacedItem item)
    {
        var itemId = item.Item.Id;
        var isWallItem = item.Item.Definition!.Type == ItemType.Wall;
        var vacated = RoomItemStore.Footprint(item).ToList();

        await using var db = await dbFactory.CreateDbContextAsync();
        // a wired copy leaves the room like any other item, but was never anything to consume
        var roomId = item.Item.RoomId;
        var deleted = !item.Temporary && await db.UserItems.Where(i => i.Id == itemId && i.RoomId == roomId).ExecuteDeleteAsync() > 0;
        items.Remove(itemId);

        // The client is told the same way a pick-up tells it, with nobody holding the item after.
        if (isWallItem)
        {
            broadcast(new ItemRemoveMessageComposer(itemId, NobodyPickedItUp));
        }
        else
        {
            broadcast(new ObjectRemoveMessageComposer(itemId, IsExpired: false, NobodyPickedItUp, Delay: 0));
            foreach (var update in HeightMapUpdateMessageComposer.Chunked(items.TileUpdates(vacated)))
                broadcast(update);
        }

        return deleted;
    }

    public async Task GiveCreditsAsync(RoomEntityStore.RoomEntity user, int credits)
    {
        var balance = await grains.GetGrain<IPlayerGrain>(user.Player.Id).AddCreditsAsync(credits, Habbo.Domain.Users.CurrencyTransactionReasons.CreditFurniRedeem);
        sendTo(user.Player.Id, new CreditBalanceComposer(balance));
    }

    public void Broadcast(IComposer composer) => broadcast(composer);

    public void Schedule(TimeSpan delay, Func<Task> action) => schedule(delay, action);

    public bool IsPlaced(RoomItemStore.PlacedItem item) => ReferenceEquals(items.Find(item.Item.Id), item);

    public bool IsWithinReach(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item) => Reaches(user, item);

    /// <summary>Whether the avatar stands on one of the item's tiles or on a tile touching one.</summary>
    public static bool Reaches(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item) =>
        RoomItemStore.Footprint(item).Any(tile =>
            Math.Abs(tile.X - user.X) <= Reach && Math.Abs(tile.Y - user.Y) <= Reach);
}

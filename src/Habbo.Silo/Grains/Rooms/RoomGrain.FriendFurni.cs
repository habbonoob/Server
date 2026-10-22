using System.Globalization;
using Habbo.Domain.Furniture.Data;
using Habbo.Protocol.Messages.Outgoing.FriendFurni;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Locking friendship furniture: a player using one with a friend beside it asks both of them, and once
/// both confirm it is engraved with their names, their looks and the day. Either turning it down, or leaving,
/// calls it off. A locked one only shows its engraving.</summary>
public sealed partial class RoomGrain
{
    /// <summary>The state a locked friendship furni shows, first of its strings.</summary>
    private const string FriendFurniLocked = "1";

    private const string FriendFurniDateFormat = "dd-MM-yyyy";

    private readonly Dictionary<int, FriendLock> _friendLocks = [];

    /// <summary>Starts a lock when the item is friendship furniture; false for anything else.</summary>
    private async Task<bool> StartFriendLockAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item)
    {
        if (item.Interaction is not FriendFurniInteraction)
            return false;
        if (item.Data.StringValues is [FriendFurniLocked, ..] || _friendLocks.ContainsKey(item.Item.Id)
            || _friendLocks.Values.Any(l => l.Involves(user.Player.Id)) || !RoomFurnitureUse.Reaches(user, item))
            return true;

        var beside = _entities!.Entities
            .Where(other => other != user && RoomFurnitureUse.Reaches(other, item) && !_friendLocks.Values.Any(l => l.Involves(other.Player.Id)))
            .ToList();
        if (beside.Count == 0)
            return true;

        var besideIds = beside.Select(other => other.Player.Id).ToList();
        await using var db = await dbFactory.CreateDbContextAsync();
        var friendIds = await db.Friendships.Where(f => f.UserId == user.Player.Id && besideIds.Contains(f.FriendId)).Select(f => f.FriendId).ToListAsync();
        if (beside.FirstOrDefault(other => friendIds.Contains(other.Player.Id)) is not { } friend)
            return true;

        var pending = new FriendLock(item.Item.Id, user.Player.Id, friend.Player.Id);
        _friendLocks[item.Item.Id] = pending;
        foreach (var playerId in new[] { pending.Starter, pending.Friend })
            SendTo(playerId, new FriendFurniStartConfirmationMessageComposer(item.Item.Id, playerId == item.Item.OwnerId));
        return true;
    }

    public async Task ConfirmFriendLockAsync(Guid playerId, int itemId, bool confirmed)
    {
        if (!_friendLocks.TryGetValue(itemId, out var pending) || !pending.Involves(playerId))
            return;

        var partner = pending.PartnerOf(playerId);
        if (!confirmed)
        {
            CancelFriendLock(pending, partner);
            return;
        }

        pending.Confirmed.Add(playerId);
        if (pending.Confirmed.Count < 2)
        {
            SendTo(partner, new FriendFurniOtherLockConfirmedMessageComposer(itemId));
            return;
        }

        _friendLocks.Remove(itemId);
        if (_items?.Find(itemId) is not { } item || _entities?.Find(pending.Starter) is not { } starter
            || _entities.Find(pending.Friend) is not { } friend)
            return;

        await _furniture!.UpdateAsync(item, FurniDataView.Plain with
        {
            Type = (int)FurniDataType.StringArray,
            State = null,
            StringValues =
            [
                FriendFurniLocked, starter.Player.Username, friend.Player.Username, starter.Figure, friend.Figure,
                DateTime.UtcNow.ToString(FriendFurniDateFormat, CultureInfo.InvariantCulture),
            ],
        });
    }

    /// <summary>A player leaving calls off any lock they were part of.</summary>
    private void LeaveFriendLocks(Guid playerId)
    {
        foreach (var pending in _friendLocks.Values.Where(l => l.Involves(playerId)).ToList())
            CancelFriendLock(pending, pending.PartnerOf(playerId));
    }

    private void CancelFriendLock(FriendLock pending, Guid tell)
    {
        _friendLocks.Remove(pending.ItemId);
        SendTo(tell, new FriendFurniCancelLockMessageComposer(pending.ItemId));
    }
}

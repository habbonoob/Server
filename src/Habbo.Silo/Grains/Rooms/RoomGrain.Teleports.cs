using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Room.Furniture;
using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Teleports and hoppers. The avatar walks up to the front, the door opens and it steps in; a teleport flashes
/// and the avatar comes out of its partner, in this room or another, and a hopper sends it to a random room with a
/// hopper of its kind. Coming out, the door opens, the avatar steps out and the door closes. A teleport's partner
/// takes the avatar in whatever the room's door says, as a linked pair always has.</summary>
public sealed partial class RoomGrain
{
    // what a teleport or hopper shows
    private const string TeleportClosed = "0";
    private const string TeleportOpen = "1";
    private const string TeleportFlashing = "2";

    private static readonly TimeSpan TeleportStepTime = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan TeleportFlashTime = TimeSpan.FromMilliseconds(1000);

    /// <summary>How long a room waits for someone on their way through a teleport into it.</summary>
    private static readonly TimeSpan ArrivalKeptFor = TimeSpan.FromSeconds(30);

    /// <summary>Who is on their way in through which teleport, and until when they are expected.</summary>
    private readonly Dictionary<Guid, (int ItemId, DateTime Until)> _arrivals = [];

    /// <summary>Who came in inside a teleport and steps out of it once their client has the room.</summary>
    private readonly Dictionary<Guid, int> _arrivingThrough = [];

    private static bool IsTeleport(RoomItemStore.PlacedItem item) => item.Interaction is TeleportInteraction or HopperInteraction;

    private async Task UseTeleportAsync(RoomEntityStore.RoomEntity entity, RoomItemStore.PlacedItem teleport)
    {
        if (teleport.IsBusy)
            return;
        if (teleport.Interaction is HopperInteraction && await HopperRefusalAsync(entity, teleport) is { } refusal)
        {
            SendTo(entity.Player.Id, new CustomUserNotificationMessageComposer(refusal));
            return;
        }

        var (frontX, frontY) = FrontOf(teleport);
        await WalkThenAsync(entity, frontX, frontY, () =>
        {
            if (entity.X != frontX || entity.Y != frontY || !_furniture!.IsPlaced(teleport) || teleport.IsBusy
                || _entities!.HasAvatarAt(teleport.X, teleport.Y))
                return Task.CompletedTask;

            teleport.BusyUntil = DateTime.UtcNow + TeleportStepTime + TeleportFlashTime + TeleportStepTime;
            ShowTeleport(teleport, TeleportOpen);
            Broadcast(UserUpdateMessageComposer.From([_entities.StepInto(entity, teleport.X, teleport.Y)]));
            Schedule(TeleportStepTime, () =>
            {
                if (!Inside(entity, teleport))
                {
                    ShowTeleport(teleport, TeleportClosed);
                    return Task.CompletedTask;
                }

                if (teleport.Interaction is TeleportInteraction)
                    ShowTeleport(teleport, TeleportFlashing);
                Schedule(TeleportFlashTime, () => TravelAsync(entity, teleport));
                return Task.CompletedTask;
            });
            return Task.CompletedTask;
        });
    }

    /// <summary>Sends the avatar on from inside the teleport: out of its partner here, into the room the partner or a
    /// hopper stands in, or back out where it went in when there is nowhere to go.</summary>
    private async Task TravelAsync(RoomEntityStore.RoomEntity entity, RoomItemStore.PlacedItem source)
    {
        if (!Inside(entity, source))
        {
            ShowTeleport(source, TeleportClosed);
            return;
        }

        var destination = source.Interaction is HopperInteraction ? await HopperDestinationAsync(source) : await PartnerOfAsync(source);
        if (destination is not { } found)
        {
            await StepOutAsync(entity, source);
            return;
        }

        if (found.RoomId == _room!.Id)
        {
            if (_items!.Find(found.ItemId) is not { } partner || partner.IsBusy || _entities!.HasAvatarAt(partner.X, partner.Y))
            {
                await StepOutAsync(entity, source);
                return;
            }

            ShowTeleport(source, TeleportClosed);
            _entities.Carry(entity, partner.X, partner.Y);
            Broadcast(UserUpdateMessageComposer.From([_entities.Turn(entity, partner.Rotation)]));
            ArriveThrough(entity, partner);
            return;
        }

        if (!await GrainFactory.GetGrain<IGameRoomGrain>(found.RoomId).ExpectArrivalAsync(entity.Player.Id, found.ItemId))
        {
            await StepOutAsync(entity, source);
            return;
        }

        ShowTeleport(source, TeleportClosed);
        SendTo(entity.Player.Id, new RoomForwardMessageComposer(found.RoomId));
    }

    /// <summary>For wired: the avatar goes straight through the teleport, without walking up to it or stepping in,
    /// and comes out of its partner, here or in the partner's room. A teleport that leads nowhere does nothing.</summary>
    private async Task SendThroughTeleportAsync(RoomEntityStore.RoomEntity entity, RoomItemStore.PlacedItem teleport)
    {
        if (!IsTeleport(teleport) || _entities is null || _items is null)
            return;

        var destination = teleport.Interaction is HopperInteraction ? await HopperDestinationAsync(teleport) : await PartnerOfAsync(teleport);
        if (destination is not { } found)
            return;

        if (found.RoomId != _room!.Id)
        {
            if (await GrainFactory.GetGrain<IGameRoomGrain>(found.RoomId).ExpectArrivalAsync(entity.Player.Id, found.ItemId))
                SendTo(entity.Player.Id, new RoomForwardMessageComposer(found.RoomId));
            return;
        }

        if (_items.Find(found.ItemId) is not { } partner || partner.IsBusy || _entities.HasAvatarAt(partner.X, partner.Y))
            return;

        _entities.Carry(entity, partner.X, partner.Y);
        Broadcast(UserUpdateMessageComposer.From([_entities.Turn(entity, partner.Rotation)]));
        ArriveThrough(entity, partner);
    }

    public Task<bool> ExpectArrivalAsync(Guid playerId, int itemId)
    {
        if (_items?.Find(itemId) is not { } teleport || !IsTeleport(teleport))
            return Task.FromResult(false);

        _arrivals[playerId] = (itemId, DateTime.UtcNow + ArrivalKeptFor);
        _admitted.Give(playerId);
        return Task.FromResult(true);
    }

    /// <summary>Puts someone arriving by teleport inside it, instead of at the door, when it is still free.</summary>
    private void PlaceArrival(RoomEntityStore.RoomEntity entity)
    {
        var playerId = entity.Player.Id;
        if (!_arrivals.Remove(playerId, out var arrival) || arrival.Until < DateTime.UtcNow
            || _items!.Find(arrival.ItemId) is not { } teleport || _entities!.HasAvatarAt(teleport.X, teleport.Y))
            return;

        _entities.Carry(entity, teleport.X, teleport.Y);
        _entities.Turn(entity, teleport.Rotation);
        _arrivingThrough[playerId] = teleport.Item.Id;
    }

    /// <summary>Once the client has the room, someone who arrived inside a teleport comes out of it.</summary>
    private void StepOutOfArrival(RoomEntityStore.RoomEntity entity)
    {
        if (_arrivingThrough.Remove(entity.Player.Id, out var itemId) && _items!.Find(itemId) is { } teleport && Inside(entity, teleport))
            ArriveThrough(entity, teleport);
    }

    private void ArriveThrough(RoomEntityStore.RoomEntity entity, RoomItemStore.PlacedItem teleport)
    {
        teleport.BusyUntil = DateTime.UtcNow + TeleportFlashTime + TeleportStepTime * 2;
        ShowTeleport(teleport, teleport.Interaction is TeleportInteraction ? TeleportFlashing : TeleportOpen);
        Schedule(TeleportFlashTime, () => StepOutAsync(entity, teleport));
    }

    /// <summary>The door opens, the avatar steps out in front, and the door closes behind it.</summary>
    private Task StepOutAsync(RoomEntityStore.RoomEntity entity, RoomItemStore.PlacedItem teleport)
    {
        if (!Inside(entity, teleport))
        {
            ShowTeleport(teleport, TeleportClosed);
            return Task.CompletedTask;
        }

        ShowTeleport(teleport, TeleportOpen);
        var (frontX, frontY) = FrontOf(teleport);
        if (_entities!.Slide(entity, frontX, frontY) is { } stepped)
            Broadcast(UserUpdateMessageComposer.From([stepped]));
        Schedule(TeleportStepTime, () =>
        {
            ShowTeleport(teleport, TeleportClosed);
            return Task.CompletedTask;
        });
        return Task.CompletedTask;
    }

    /// <summary>Where a teleport's partner stands: its room and its id; none when it is in nobody's room.</summary>
    private async Task<(int RoomId, int ItemId)?> PartnerOfAsync(RoomItemStore.PlacedItem teleport)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var partner = await db.FurniTeleportLinks.AsNoTracking()
            .Where(link => link.ItemId == teleport.Item.Id)
            .Select(link => new { link.LinkedItemId, link.LinkedItem!.RoomId })
            .FirstOrDefaultAsync();
        return partner is { RoomId: { } roomId } ? (roomId, partner.LinkedItemId) : null;
    }

    /// <summary>A hopper of the same kind in another room whose door is open, picked at random.</summary>
    private async Task<(int RoomId, int ItemId)?> HopperDestinationAsync(RoomItemStore.PlacedItem hopper)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var hoppers = await db.UserItems.AsNoTracking()
            .Where(item => item.DefinitionId == hopper.Item.DefinitionId && item.RoomId != null && item.RoomId != _room!.Id)
            .Join(db.Rooms.Where(room => room.State == RoomState.Open), item => item.RoomId, room => room.Id, (item, room) => new { item.Id, RoomId = room.Id })
            .ToListAsync();
        if (hoppers.Count == 0)
            return null;

        var picked = hoppers[Random.Shared.Next(hoppers.Count)];
        return (picked.RoomId, picked.Id);
    }

    /// <summary>Why the avatar may not use the hopper, as the notice the client shows; null when it may.</summary>
    private async Task<int?> HopperRefusalAsync(RoomEntityStore.RoomEntity entity, RoomItemStore.PlacedItem hopper)
    {
        if (!roomOptions.Value.Hoppers.TryGetValue(hopper.Item.Definition!.ClassName, out var rule))
            return null;
        if (rule.ClubOnly && entity.Player.ClubLevel == ClubLevel.None)
            return CustomUserNotificationMessageComposer.VipHopper;
        if (!rule.CostumeOnly)
            return null;

        foreach (var costume in roomOptions.Value.CostumeEffects)
        {
            if (entity.EffectId != 0 && await avatarEffects.FindAsync(costume) == entity.EffectId)
                return null;
        }

        return CustomUserNotificationMessageComposer.CostumeHopper;
    }

    private bool Inside(RoomEntityStore.RoomEntity entity, RoomItemStore.PlacedItem teleport) =>
        _entities?.Find(entity.Player.Id) == entity && entity.X == teleport.X && entity.Y == teleport.Y && _furniture!.IsPlaced(teleport);

    private static (int X, int Y) FrontOf(RoomItemStore.PlacedItem teleport)
    {
        var (dx, dy) = DirectionStep(teleport.Rotation);
        return (teleport.X + dx, teleport.Y + dy);
    }

    /// <summary>Shows the teleport's door and light; only the room sees it, nothing is stored.</summary>
    private void ShowTeleport(RoomItemStore.PlacedItem teleport, string state)
    {
        if (teleport.Data.State == state || !_furniture!.IsPlaced(teleport))
            return;

        teleport.Data = teleport.Data with { State = state };
        _furniture.ShowData(teleport);
    }
}

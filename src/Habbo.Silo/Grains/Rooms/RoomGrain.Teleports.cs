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

/// <summary>Teleports and hoppers. The avatar walks up to the front, the door opens as it takes its last step there,
/// and it steps in; the door closes behind it, the teleport flashes, its partner flashes too, and the avatar comes out
/// of the partner, in this room or another, and a hopper sends it to a random room with a hopper of its kind. Coming
/// out, the door opens, the avatar steps out and the door closes. The timings are Habbo's, measured on habbo.com
/// (October 2026). A teleport's partner takes the avatar in whatever the room's door says, as a linked pair always
/// has.</summary>
public sealed partial class RoomGrain
{
    // what a teleport or hopper shows
    private const string TeleportClosed = "0";
    private const string TeleportOpen = "1";
    private const string TeleportFlashing = "2";

    /// <summary>How far into the avatar's last step to the front the door opens.</summary>
    private static readonly TimeSpan DoorOpensAhead = TimeSpan.FromMilliseconds(250);

    // going in, from the moment the avatar steps into the teleport (it stands inside half a second later)
    private static readonly TimeSpan DoorClosesBehind = TimeSpan.FromMilliseconds(1250);
    private static readonly TimeSpan FlashesAt = TimeSpan.FromMilliseconds(2000);

    /// <summary>Where the avatar goes is settled, and one going to another room is sent there.</summary>
    private static readonly TimeSpan LeavesAt = TimeSpan.FromMilliseconds(2300);

    /// <summary>A partner in this room flashes and the avatar, still inside, turns the way it will come out.</summary>
    private static readonly TimeSpan TravelsAt = TimeSpan.FromMilliseconds(2500);

    private static readonly TimeSpan GoesDarkAt = TimeSpan.FromMilliseconds(2750);
    private static readonly TimeSpan ArrivesAt = TimeSpan.FromMilliseconds(3000);

    // coming out, from the moment the avatar is inside the partner: in this room, and in a room it was sent to,
    // timed from the room reaching its client
    private static readonly (TimeSpan DoorOpens, TimeSpan StepsOut) ComingOutHere =
        (TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(1000));
    private static readonly (TimeSpan DoorOpens, TimeSpan StepsOut) ComingOutElsewhere =
        (TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(1500));

    /// <summary>How long after the avatar steps out the door closes behind it.</summary>
    private static readonly TimeSpan DoorClosesAfterStep = TimeSpan.FromMilliseconds(750);

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
        Func<Task> enter = () => EnterAsync(entity, teleport, frontX, frontY);
        await WalkThenAsync(entity, frontX, frontY, enter);
        if (entity.OnArrival != enter)
            return;

        // still on the way: the door opens while the avatar takes its last step to the front
        entity.OnLastStep = () =>
        {
            Schedule(DoorOpensAhead, () =>
            {
                if (entity.OnArrival == enter && entity.X == frontX && entity.Y == frontY && !teleport.IsBusy
                    && !_entities!.HasAvatarAt(teleport.X, teleport.Y))
                    ShowTeleport(teleport, TeleportOpen);
                return Task.CompletedTask;
            });
            return Task.CompletedTask;
        };
    }

    /// <summary>The avatar, in front, steps in; the door closes behind it and the teleport flashes before it
    /// travels.</summary>
    private Task EnterAsync(RoomEntityStore.RoomEntity entity, RoomItemStore.PlacedItem teleport, int frontX, int frontY)
    {
        if (!_furniture!.IsPlaced(teleport) || teleport.IsBusy || _entities!.HasAvatarAt(teleport.X, teleport.Y))
            return Task.CompletedTask;
        if (entity.X != frontX || entity.Y != frontY)
        {
            // the door opened for a walk that ended short of it
            ShowTeleport(teleport, TeleportClosed);
            return Task.CompletedTask;
        }

        teleport.BusyUntil = DateTime.UtcNow + ArrivesAt;
        ShowTeleport(teleport, TeleportOpen);
        BroadcastStep(entity, _entities.StepInto(entity, teleport.X, teleport.Y));
        Schedule(DoorClosesBehind, () =>
        {
            ShowTeleport(teleport, TeleportClosed);
            if (!Inside(entity, teleport))
                return Task.CompletedTask;

            Schedule(FlashesAt - DoorClosesBehind, () =>
            {
                if (!Inside(entity, teleport))
                    return Task.CompletedTask;

                if (teleport.Interaction is TeleportInteraction)
                    ShowTeleport(teleport, TeleportFlashing);
                Schedule(LeavesAt - FlashesAt, () => TravelAsync(entity, teleport));
                return Task.CompletedTask;
            });
            return Task.CompletedTask;
        });
        return Task.CompletedTask;
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

        TrackAction(entity, Habbo.Silo.RewardTracks.RewardTrackActions.Teleport);

        if (found.RoomId == _room!.Id)
        {
            Schedule(TravelsAt - LeavesAt, () => TravelHereAsync(entity, source, found.ItemId));
            return;
        }

        if (!await GrainFactory.GetGrain<IGameRoomGrain>(found.RoomId).ExpectArrivalAsync(entity.Player.Id, found.ItemId))
        {
            await StepOutAsync(entity, source);
            return;
        }

        Schedule(GoesDarkAt - LeavesAt, () =>
        {
            ShowTeleport(source, TeleportClosed);
            return Task.CompletedTask;
        });
        SendTo(entity.Player.Id, new RoomForwardMessageComposer(found.RoomId));
    }

    /// <summary>To a partner in this room: it lights up and the avatar turns the way it will come out, then the source
    /// goes dark and the avatar is in the partner.</summary>
    private Task TravelHereAsync(RoomEntityStore.RoomEntity entity, RoomItemStore.PlacedItem source, int partnerId)
    {
        if (!Inside(entity, source))
        {
            ShowTeleport(source, TeleportClosed);
            return Task.CompletedTask;
        }

        if (_items!.FindStored(partnerId) is not { } partner || partner.IsBusy || _entities!.HasAvatarAt(partner.X, partner.Y))
            return StepOutAsync(entity, source);

        partner.BusyUntil = DateTime.UtcNow + (ArrivesAt - TravelsAt) + ComingOutHere.StepsOut + DoorClosesAfterStep;
        if (partner.Interaction is TeleportInteraction)
            ShowTeleport(partner, TeleportFlashing);
        Broadcast(UserUpdateMessageComposer.From([_entities.Turn(entity, partner.Rotation)]));
        Schedule(GoesDarkAt - TravelsAt, () =>
        {
            ShowTeleport(source, TeleportClosed);
            return Task.CompletedTask;
        });
        Schedule(ArrivesAt - TravelsAt, () =>
        {
            if (!Inside(entity, source) || !_furniture!.IsPlaced(partner) || _entities.HasAvatarAt(partner.X, partner.Y))
            {
                ShowTeleport(partner, TeleportClosed);
                return Inside(entity, source) ? StepOutAsync(entity, source) : Task.CompletedTask;
            }

            _entities.Carry(entity, partner.X, partner.Y);
            Broadcast(UserUpdateMessageComposer.From([_entities.Turn(entity, partner.Rotation)]));
            ComeOut(entity, partner, ComingOutHere);
            return Task.CompletedTask;
        });
        return Task.CompletedTask;
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

        if (_items.FindStored(found.ItemId) is not { } partner || partner.IsBusy || _entities.HasAvatarAt(partner.X, partner.Y))
            return;

        _entities.Carry(entity, partner.X, partner.Y);
        Broadcast(UserUpdateMessageComposer.From([_entities.Turn(entity, partner.Rotation)]));
        if (partner.Interaction is TeleportInteraction)
            ShowTeleport(partner, TeleportFlashing);
        ComeOut(entity, partner, ComingOutHere);
    }

    /// <summary>Someone is on their way through a teleport here, named by its database id as its partner's link (or
    /// a hopper's search) found it; kept by the id the room knows it by.</summary>
    public Task<bool> ExpectArrivalAsync(Guid playerId, int itemId)
    {
        if (_items?.FindStored(itemId) is not { } teleport || !IsTeleport(teleport))
            return Task.FromResult(false);

        _arrivals[playerId] = (teleport.Id, DateTime.UtcNow + ArrivalKeptFor);
        _admitted.Give(playerId);
        return Task.FromResult(true);
    }

    /// <summary>Puts someone arriving by teleport inside it, instead of at the door, when it is still free.</summary>
    private void PlaceArrival(RoomEntityStore.RoomEntity entity)
    {
        var playerId = entity.Player.Id;
        RememberEntry(entity, null);
        if (!_arrivals.Remove(playerId, out var arrival) || arrival.Until < DateTime.UtcNow
            || _items!.Find(arrival.ItemId) is not { } teleport || _entities!.HasAvatarAt(teleport.X, teleport.Y))
            return;

        RememberEntry(entity, teleport.Id);

        _entities.Carry(entity, teleport.X, teleport.Y);
        _entities.Turn(entity, teleport.Rotation);
        _arrivingThrough[playerId] = teleport.Id;
    }

    /// <summary>Once the client has the room, someone who arrived inside a teleport comes out of it.</summary>
    private void StepOutOfArrival(RoomEntityStore.RoomEntity entity)
    {
        if (!_arrivingThrough.Remove(entity.Player.Id, out var itemId) || _items!.Find(itemId) is not { } teleport
            || !Inside(entity, teleport))
            return;

        if (teleport.Interaction is TeleportInteraction)
            ShowTeleport(teleport, TeleportFlashing);
        ComeOut(entity, teleport, ComingOutElsewhere);
    }

    /// <summary>The avatar is inside: the door opens, the avatar steps out in front, and the door closes behind
    /// it.</summary>
    private void ComeOut(RoomEntityStore.RoomEntity entity, RoomItemStore.PlacedItem teleport,
        (TimeSpan DoorOpens, TimeSpan StepsOut) timing)
    {
        teleport.BusyUntil = DateTime.UtcNow + timing.StepsOut + DoorClosesAfterStep;
        Schedule(timing.DoorOpens, () =>
        {
            if (!Inside(entity, teleport))
            {
                ShowTeleport(teleport, TeleportClosed);
                return Task.CompletedTask;
            }

            ShowTeleport(teleport, TeleportOpen);
            Schedule(timing.StepsOut - timing.DoorOpens, () => StepOutAsync(entity, teleport));
            return Task.CompletedTask;
        });
    }

    /// <summary>The door opens if it is not yet, the avatar steps out in front, and the door closes behind it.</summary>
    private Task StepOutAsync(RoomEntityStore.RoomEntity entity, RoomItemStore.PlacedItem teleport)
    {
        if (!Inside(entity, teleport))
        {
            ShowTeleport(teleport, TeleportClosed);
            return Task.CompletedTask;
        }

        ShowTeleport(teleport, TeleportOpen);
        var (frontX, frontY) = FrontOf(teleport);
        if (_entities!.Slide(entity, frontX, frontY, facing: true) is { } stepped)
            BroadcastStep(entity, stepped);
        Schedule(DoorClosesAfterStep, () =>
        {
            ShowTeleport(teleport, TeleportClosed);
            return Task.CompletedTask;
        });
        return Task.CompletedTask;
    }

    /// <summary>Where a teleport's partner stands: its room and its database id; none when it is in nobody's room.</summary>
    private async Task<(int RoomId, int ItemId)?> PartnerOfAsync(RoomItemStore.PlacedItem teleport)
    {
        // wired may have pointed it at another teleport here
        if (WiredTargetOf(teleport) is { } wired)
            return wired;

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

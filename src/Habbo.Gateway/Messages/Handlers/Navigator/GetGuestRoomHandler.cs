using Habbo.Domain.Rooms;
using Habbo.Gateway.Messages.Handlers.Room.Session;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Protocol.Messages.Outgoing.Navigator;
using Habbo.Silo.Abstractions.Rooms;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>3176 GetGuestRoom — the "can I enter" round-trip. A navigator click arrives as
/// (enterRoom=0, forwardRoom=1), and an open room is entered there and then; after RoomEntryInfo the client
/// re-asks with enterRoom=1.</summary>
internal sealed class GetGuestRoomHandler(IGrainFactory grains) : IMessageHandler<GetGuestRoomMessageEvent>
{
    /// <summary>The client sends these two flags as ints rather than booleans.</summary>
    private const int Yes = 1;

    public async Task<bool> HandleAsync(
        GatewaySessionContext context, GetGuestRoomMessageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null)
            return false;

        var info = await grains.GetGrain<IGameRoomGrain>(message.RoomId).GetInfoAsync(player.Id);
        if (info is null)
            return true; // the hotel view tolerates silence for an unknown room

        // A forward to an open room is opened by the server there and then, as Habbo does: openingConnection tells
        // the client to skip OpenFlatConnection and wait for the room. Behind a doorbell or a password the client
        // asks first, and sends OpenFlatConnection itself.
        var forward = message.ForwardRoom == Yes;
        var opening = forward && info.DoorMode == (int)RoomState.Open;
        await context.SendAsync(
            GetGuestRoomResultComposer.From(message.EnterRoom == Yes, info, forward, opening), ct);
        if (opening)
            await RoomEntry.EnterAsync(grains, context, message.RoomId, password: string.Empty, ct);
        return true;
    }
}

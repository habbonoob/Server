using Habbo.Protocol.Messages.Incoming.RoomSettings;
using Habbo.Silo.Abstractions.Rooms;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.RoomSettings;

/// <summary>DeleteRoomMessageEvent — the owner deletes a room; whoever was in it is sent out.</summary>
internal sealed class DeleteRoomHandler(IGrainFactory grains) : IMessageHandler<DeleteRoomMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, DeleteRoomMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        var room = grains.GetGrain<IGameRoomGrain>(message.RoomId);
        if (await room.DeleteAsync(player) && context.Session.Room?.GetPrimaryKeyLong() == message.RoomId)
            await context.Session.SetRoomAsync(null);
        return true;
    }
}

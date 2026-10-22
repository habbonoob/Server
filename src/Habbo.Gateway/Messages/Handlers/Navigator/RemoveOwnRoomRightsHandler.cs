using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>2073 RemoveOwnRoomRightsRoom — [roomId].</summary>
internal sealed class RemoveOwnRoomRightsHandler(IGrainFactory grains) : IMessageHandler<RemoveOwnRoomRightsRoomMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RemoveOwnRoomRightsRoomMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await grains.GetGrain<IGameRoomGrain>(message.RoomId).RemoveOwnRightsAsync(player.Id);
        return true;
    }
}

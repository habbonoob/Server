using Habbo.Protocol.Messages.Incoming.Room.Action;
using Habbo.Silo.Abstractions.Rooms;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Room.Action;

/// <summary>UnbanUserFromRoomMessageEvent — lift a room ban.</summary>
internal sealed class UnbanUserFromRoomHandler(IGrainFactory service) : IMessageHandler<UnbanUserFromRoomMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UnbanUserFromRoomMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGameRoomGrain>(message.RoomId).UnbanUserAsync(player.Id, message.UserId);
        return true;
    }
}

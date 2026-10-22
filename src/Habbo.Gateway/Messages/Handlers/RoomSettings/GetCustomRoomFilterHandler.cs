using Habbo.Protocol.Messages.Incoming.RoomSettings;
using Habbo.Silo.Abstractions.Rooms;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.RoomSettings;

/// <summary>GetCustomRoomFilterMessageEvent — the words a room filters.</summary>
internal sealed class GetCustomRoomFilterHandler(IGrainFactory service) : IMessageHandler<GetCustomRoomFilterMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetCustomRoomFilterMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGameRoomGrain>(message.RoomId).SendFilterAsync(player);
        return true;
    }
}

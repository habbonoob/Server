using Habbo.Protocol.Messages.Incoming.RoomSettings;
using Habbo.Silo.Abstractions.Rooms;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.RoomSettings;

/// <summary>UpdateRoomFilterMessageEvent — add or remove a filtered word.</summary>
internal sealed class UpdateRoomFilterHandler(IGrainFactory service) : IMessageHandler<UpdateRoomFilterMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UpdateRoomFilterMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGameRoomGrain>(message.RoomId).UpdateFilterAsync(player, message.Add, message.Word);
        return true;
    }
}

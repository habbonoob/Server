using Habbo.Protocol.Messages.Incoming.RoomSettings;
using Habbo.Silo.Abstractions.Rooms;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.RoomSettings;

/// <summary>GetBannedUsersFromRoomMessageEvent — who is banned from a room.</summary>
internal sealed class GetBannedUsersFromRoomHandler(IGrainFactory service) : IMessageHandler<GetBannedUsersFromRoomMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetBannedUsersFromRoomMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGameRoomGrain>(message.RoomId).SendBannedUsersAsync(player.Id);
        return true;
    }
}

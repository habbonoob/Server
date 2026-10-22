using Habbo.Protocol.Messages.Incoming.RoomSettings;
using Habbo.Silo.Abstractions.Rooms;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.RoomSettings;

/// <summary>GetRoomSettingsMessageEvent — a room's settings.</summary>
internal sealed class GetRoomSettingsHandler(IGrainFactory service) : IMessageHandler<GetRoomSettingsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetRoomSettingsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGameRoomGrain>(message.RoomId).SendSettingsAsync(player);
        return true;
    }
}

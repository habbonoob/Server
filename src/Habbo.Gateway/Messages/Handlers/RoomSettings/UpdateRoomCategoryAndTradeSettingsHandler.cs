using Habbo.Protocol.Messages.Incoming.RoomSettings;
using Habbo.Silo.Abstractions.Rooms;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.RoomSettings;

/// <summary>UpdateRoomCategoryAndTradeSettingsEvent — a room's category and trading.</summary>
internal sealed class UpdateRoomCategoryAndTradeSettingsHandler(IGrainFactory service) : IMessageHandler<UpdateRoomCategoryAndTradeSettingsEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UpdateRoomCategoryAndTradeSettingsEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGameRoomGrain>(message.RoomId).UpdateCategoryAndTradeAsync(player, message.CategoryId, message.TradeMode);
        return true;
    }
}

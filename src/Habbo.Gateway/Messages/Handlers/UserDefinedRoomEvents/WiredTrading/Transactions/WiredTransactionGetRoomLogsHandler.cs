using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Transactions;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Transactions;

/// <summary>WiredTransactionGetRoomLogsMessageEvent — the room's chest transactions.</summary>
internal sealed class WiredTransactionGetRoomLogsHandler : IMessageHandler<WiredTransactionGetRoomLogsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredTransactionGetRoomLogsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SendRoomChestLogsAsync(player.Id, message.PageSize, message.Page);
        return true;
    }
}

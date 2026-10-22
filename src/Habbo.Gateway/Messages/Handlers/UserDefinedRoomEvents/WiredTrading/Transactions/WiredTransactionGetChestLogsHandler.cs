using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Transactions;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Transactions;

/// <summary>WiredTransactionGetChestLogsMessageEvent — a chest's transactions.</summary>
internal sealed class WiredTransactionGetChestLogsHandler : IMessageHandler<WiredTransactionGetChestLogsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredTransactionGetChestLogsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SendChestLogsAsync(player.Id, message.ChestId, message.PageSize, message.Page);
        return true;
    }
}

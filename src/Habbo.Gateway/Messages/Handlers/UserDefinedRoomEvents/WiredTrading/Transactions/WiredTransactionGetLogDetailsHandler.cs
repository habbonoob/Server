using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Transactions;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Transactions;

/// <summary>WiredTransactionGetLogDetailsMessageEvent — one transaction in full.</summary>
internal sealed class WiredTransactionGetLogDetailsHandler : IMessageHandler<WiredTransactionGetLogDetailsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredTransactionGetLogDetailsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SendChestLogDetailsAsync(player.Id, message.TransactionId);
        return true;
    }
}

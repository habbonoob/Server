using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>WithdrawCoinsFromChestMessageEvent — take credits out of a chest.</summary>
internal sealed class WithdrawCoinsFromChestHandler : IMessageHandler<WithdrawCoinsFromChestMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WithdrawCoinsFromChestMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.WithdrawChestCoinsAsync(player.Id, message.ChestId, message.Amount);
        return true;
    }
}

using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>WithdrawAllFromChestMessageEvent — empty a chest.</summary>
internal sealed class WithdrawAllFromChestHandler : IMessageHandler<WithdrawAllFromChestMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WithdrawAllFromChestMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.WithdrawAllFromChestAsync(player.Id, message.ChestId);
        return true;
    }
}

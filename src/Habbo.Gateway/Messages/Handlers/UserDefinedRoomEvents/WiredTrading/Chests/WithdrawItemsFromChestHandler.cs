using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>WithdrawItemsFromChestMessageEvent — take items out of a chest.</summary>
internal sealed class WithdrawItemsFromChestHandler : IMessageHandler<WithdrawItemsFromChestMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WithdrawItemsFromChestMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.WithdrawChestItemsAsync(player.Id, message.ChestId, message.IsWallItem, message.TypeId, message.PosterId, message.Amount);
        return true;
    }
}

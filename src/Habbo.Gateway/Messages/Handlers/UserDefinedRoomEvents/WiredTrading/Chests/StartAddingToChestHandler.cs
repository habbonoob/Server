using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>StartAddingToChestMessageEvent — start depositing into a chest.</summary>
internal sealed class StartAddingToChestHandler : IMessageHandler<StartAddingToChestMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, StartAddingToChestMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.StartChestDepositAsync(player.Id, message.ChestId);
        return true;
    }
}

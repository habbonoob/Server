using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>UpgradeChestMessageEvent — buy more room in a chest.</summary>
internal sealed class UpgradeChestHandler : IMessageHandler<UpgradeChestMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UpgradeChestMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.UpgradeChestAsync(player.Id, message.ChestId, message.Levels);
        return true;
    }
}

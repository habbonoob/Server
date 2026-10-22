using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>OpenChestAndGetContentsMessageEvent — open a chest.</summary>
internal sealed class OpenChestAndGetContentsHandler : IMessageHandler<OpenChestAndGetContentsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, OpenChestAndGetContentsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.OpenChestAsync(player.Id, message.ChestId);
        return true;
    }
}

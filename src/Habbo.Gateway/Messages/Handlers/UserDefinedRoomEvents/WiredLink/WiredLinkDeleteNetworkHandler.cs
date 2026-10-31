using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredLink;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkDeleteNetworkMessageEvent — a network deleted from a Link's dialog.</summary>
internal sealed class WiredLinkDeleteNetworkHandler : IMessageHandler<WiredLinkDeleteNetworkMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredLinkDeleteNetworkMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.DeleteWiredLinkNetworkAsync(player.Id, message.ItemId, message.NetworkId);
        return true;
    }
}

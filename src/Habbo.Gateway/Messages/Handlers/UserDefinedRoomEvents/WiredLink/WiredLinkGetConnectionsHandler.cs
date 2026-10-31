using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredLink;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkGetConnectionsMessageEvent — the Links connected to a network, for a Link's dialog.</summary>
internal sealed class WiredLinkGetConnectionsHandler : IMessageHandler<WiredLinkGetConnectionsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredLinkGetConnectionsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.GetWiredLinkConnectionsAsync(player.Id, message.ItemId, message.NetworkId);
        return true;
    }
}

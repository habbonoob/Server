using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredLink;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkRenameNetworkMessageEvent — a network renamed from a Link's dialog.</summary>
internal sealed class WiredLinkRenameNetworkHandler : IMessageHandler<WiredLinkRenameNetworkMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredLinkRenameNetworkMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.RenameWiredLinkNetworkAsync(player.Id, message.ItemId, message.NetworkId, message.Name);
        return true;
    }
}

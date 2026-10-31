using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredLink;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkCreateNetworkMessageEvent — a new network, from a Link's dialog.</summary>
internal sealed class WiredLinkCreateNetworkHandler : IMessageHandler<WiredLinkCreateNetworkMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredLinkCreateNetworkMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.CreateWiredLinkNetworkAsync(player.Id, message.ItemId, message.Name);
        return true;
    }
}

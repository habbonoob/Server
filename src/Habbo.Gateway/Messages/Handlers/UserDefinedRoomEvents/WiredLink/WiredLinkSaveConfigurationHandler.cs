using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredLink;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkSaveConfigurationMessageEvent — a Link's network and whether it receives its own room's messages.</summary>
internal sealed class WiredLinkSaveConfigurationHandler : IMessageHandler<WiredLinkSaveConfigurationMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredLinkSaveConfigurationMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SaveWiredLinkAsync(player.Id, message.ItemId, message.NetworkId, message.ReceiveLocal);
        return true;
    }
}

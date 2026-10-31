using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredLink;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkOpenMessageEvent — a WIRED Link's owner asks for its dialog.</summary>
internal sealed class WiredLinkOpenHandler : IMessageHandler<WiredLinkOpenMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredLinkOpenMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.OpenWiredLinkAsync(player.Id, message.ItemId);
        return true;
    }
}

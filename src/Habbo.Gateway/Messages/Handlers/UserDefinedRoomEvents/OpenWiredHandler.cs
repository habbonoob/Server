using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents;

/// <summary>1955 Open — the client asks for a wired box's dialog.</summary>
internal sealed class OpenWiredHandler : IMessageHandler<OpenMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, OpenMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.OpenWiredAsync(player.Id, message.ItemId);
        return true;
    }
}

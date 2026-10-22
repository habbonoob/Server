using Habbo.Protocol.Messages.Incoming.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>EditEventMessageEvent — rename the room's promotion.</summary>
internal sealed class EditEventHandler : IMessageHandler<EditEventMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, EditEventMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.EditEventAsync(player, message.EventId, message.Name, message.Description);
        return true;
    }
}

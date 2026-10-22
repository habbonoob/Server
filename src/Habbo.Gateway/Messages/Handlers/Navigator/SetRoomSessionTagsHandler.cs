using Habbo.Protocol.Messages.Incoming.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>SetRoomSessionTagsMessageEvent — where the visitor came in from.</summary>
internal sealed class SetRoomSessionTagsHandler : IMessageHandler<SetRoomSessionTagsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetRoomSessionTagsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.RecordEntrySourceAsync(player.Id, message.Tag1, message.Tag2);
        return true;
    }
}

using Habbo.Protocol.Messages.Incoming.Room;

namespace Habbo.Gateway.Messages.Handlers.Room;

/// <summary>ClickCharacterEvent — turn to a clicked avatar.</summary>
internal sealed class ClickCharacterHandler : IMessageHandler<ClickCharacterEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ClickCharacterEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ClickCharacterAsync(player.Id, message.RoomIndex);
        return true;
    }
}

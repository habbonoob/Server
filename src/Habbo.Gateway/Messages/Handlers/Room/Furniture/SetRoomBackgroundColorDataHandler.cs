using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>SetRoomBackgroundColorDataEvent — the background toner's colour.</summary>
internal sealed class SetRoomBackgroundColorDataHandler : IMessageHandler<SetRoomBackgroundColorDataEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetRoomBackgroundColorDataEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetBackgroundColorAsync(player.Id, message.ItemId, message.Hue, message.Saturation, message.Lightness);
        return true;
    }
}

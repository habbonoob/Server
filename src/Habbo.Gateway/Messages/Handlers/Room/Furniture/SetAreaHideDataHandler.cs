using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>SetAreaHideDataEvent — the area an area hider hides.</summary>
internal sealed class SetAreaHideDataHandler : IMessageHandler<SetAreaHideDataEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetAreaHideDataEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetAreaHideAsync(player.Id, message.ItemId, message.RootX, message.RootY, message.Width, message.Length, message.Invisibility, message.WallItems, message.Invert);
        return true;
    }
}

using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>345 ClickFurni — [itemId, type] (a negative id for a wall item). The room's wired hears
/// the click.</summary>
internal sealed class ClickFurniHandler : IMessageHandler<ClickFurniMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ClickFurniMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        // the client sends some ids negated; int.MinValue has no positive, and Math.Abs would throw on it
        if (message.ItemId == int.MinValue)
            return true;

        await room.ClickFurniAsync(player.Id, Math.Abs(message.ItemId));
        return true;
    }
}

using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>PresentOpenMessageEvent — open a present.</summary>
internal sealed class PresentOpenHandler : IMessageHandler<PresentOpenMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PresentOpenMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.OpenPresentAsync(player.Id, message.ItemId);
        return true;
    }
}

using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>AddSpamWallPostItMessageEvent — stick up a note written beforehand.</summary>
internal sealed class AddSpamWallPostItHandler : IMessageHandler<AddSpamWallPostItMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, AddSpamWallPostItMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.AddSpamWallPostItAsync(player.Id, message.ItemId, message.Location, message.ColorHex, message.Text);
        return true;
    }
}

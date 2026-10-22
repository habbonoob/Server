using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>GetItemDataMessageEvent — read a note.</summary>
internal sealed class GetItemDataHandler : IMessageHandler<GetItemDataMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetItemDataMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SendItemDataAsync(player.Id, message.ItemId);
        return true;
    }
}

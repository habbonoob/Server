using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>SetItemDataMessageEvent — rewrite a note.</summary>
internal sealed class SetItemDataHandler : IMessageHandler<SetItemDataMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetItemDataMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetItemDataAsync(player.Id, message.ItemId, message.ColorHex, message.Text);
        return true;
    }
}

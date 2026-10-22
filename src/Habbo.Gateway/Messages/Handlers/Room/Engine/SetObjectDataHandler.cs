using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>SetObjectDataMessageEvent — an item's map data.</summary>
internal sealed class SetObjectDataHandler : IMessageHandler<SetObjectDataMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetObjectDataMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetObjectDataAsync(player, message.ItemId, message.KeysAndValues);
        return true;
    }
}

using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>SetClothingChangeDataMessageEvent — a changing gate's outfit.</summary>
internal sealed class SetClothingChangeDataHandler : IMessageHandler<SetClothingChangeDataMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetClothingChangeDataMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetClothingChangeDataAsync(player.Id, message.ItemId, message.Gender, message.Figure);
        return true;
    }
}

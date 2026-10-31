using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents;

/// <summary>SetFurnitureLabelMessageEvent — rewrite a wired label.</summary>
internal sealed class SetFurnitureLabelHandler : IMessageHandler<SetFurnitureLabelMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetFurnitureLabelMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetFurnitureLabelAsync(player.Id, message.ItemId, message.Text);
        return true;
    }
}

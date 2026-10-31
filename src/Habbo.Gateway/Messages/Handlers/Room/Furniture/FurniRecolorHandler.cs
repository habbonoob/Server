using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>FurniRecolorMessageEvent — the recolour window's apply.</summary>
internal sealed class FurniRecolorHandler : IMessageHandler<FurniRecolorMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, FurniRecolorMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.RecolorFurniAsync(player.Id, message.ItemId, message.Scope, message.Red, message.Green, message.Blue);
        return true;
    }
}

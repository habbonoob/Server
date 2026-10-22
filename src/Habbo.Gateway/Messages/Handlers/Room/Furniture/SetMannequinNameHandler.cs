using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>SetMannequinNameEvent — name a mannequin's outfit.</summary>
internal sealed class SetMannequinNameHandler : IMessageHandler<SetMannequinNameEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetMannequinNameEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetMannequinNameAsync(player.Id, message.ItemId, message.Name);
        return true;
    }
}

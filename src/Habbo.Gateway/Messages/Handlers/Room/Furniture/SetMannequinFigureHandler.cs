using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>SetMannequinFigureEvent — dress a mannequin in one's own clothes.</summary>
internal sealed class SetMannequinFigureHandler : IMessageHandler<SetMannequinFigureEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetMannequinFigureEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetMannequinFigureAsync(player.Id, message.ItemId);
        return true;
    }
}

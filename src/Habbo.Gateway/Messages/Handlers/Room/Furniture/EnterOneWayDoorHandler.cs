using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>EnterOneWayDoorMessageEvent — walk through a one-way door.</summary>
internal sealed class EnterOneWayDoorHandler : IMessageHandler<EnterOneWayDoorMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, EnterOneWayDoorMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.EnterOneWayDoorAsync(player.Id, message.ItemId);
        return true;
    }
}

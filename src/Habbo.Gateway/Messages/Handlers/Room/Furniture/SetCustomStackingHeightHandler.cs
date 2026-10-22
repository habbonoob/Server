using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>SetCustomStackingHeightEvent — a stack helper's height.</summary>
internal sealed class SetCustomStackingHeightHandler : IMessageHandler<SetCustomStackingHeightEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetCustomStackingHeightEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetStackHeightAsync(player.Id, message.ItemId, message.Height, message.MultiWalk);
        return true;
    }
}

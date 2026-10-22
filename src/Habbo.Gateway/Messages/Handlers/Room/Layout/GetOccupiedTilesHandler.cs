using Habbo.Protocol.Messages.Incoming.Room.Layout;

namespace Habbo.Gateway.Messages.Handlers.Room.Layout;

/// <summary>GetOccupiedTilesMessageEvent — the tiles furniture stands on.</summary>
internal sealed class GetOccupiedTilesHandler : IMessageHandler<GetOccupiedTilesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetOccupiedTilesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SendOccupiedTilesAsync(player.Id);
        return true;
    }
}

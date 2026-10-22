using Habbo.Protocol.Messages.Incoming.Game.Ingame;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Game.Ingame;

/// <summary>1885 Game2SetUserMoveTarget — walk towards a point.</summary>
internal sealed class Game2SetUserMoveTargetHandler(IGrainFactory grains) : IMessageHandler<Game2SetUserMoveTargetMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2SetUserMoveTargetMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await SnowWarGames.ForPlayerAsync(grains, player.Id, game => game.MoveAsync(player.Id, message.X, message.Y));
        return true;
    }
}

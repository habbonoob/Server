using Habbo.Protocol.Messages.Incoming.Game.Ingame;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Game.Ingame;

/// <summary>2055 Game2ThrowSnowballAtPosition — throw at a point.</summary>
internal sealed class Game2ThrowSnowballAtPositionHandler(IGrainFactory grains) : IMessageHandler<Game2ThrowSnowballAtPositionMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2ThrowSnowballAtPositionMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await SnowWarGames.ForPlayerAsync(grains, player.Id, game => game.ThrowAtPositionAsync(player.Id, message.X, message.Y, message.Trajectory));
        return true;
    }
}

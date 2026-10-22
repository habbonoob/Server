using Habbo.Protocol.Messages.Incoming.Game.Ingame;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Game.Ingame;

/// <summary>3424 Game2ThrowSnowballAtHuman — throw at a player.</summary>
internal sealed class Game2ThrowSnowballAtHumanHandler(IGrainFactory grains) : IMessageHandler<Game2ThrowSnowballAtHumanMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2ThrowSnowballAtHumanMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await SnowWarGames.ForPlayerAsync(grains, player.Id, game => game.ThrowAtHumanAsync(player.Id, message.TargetHumanId, message.Trajectory));
        return true;
    }
}

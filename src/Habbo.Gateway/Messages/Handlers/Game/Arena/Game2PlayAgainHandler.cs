using Habbo.Protocol.Messages.Incoming.Game.Arena;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Game.Arena;

/// <summary>1884 Game2PlayAgain — play again after the results.</summary>
internal sealed class Game2PlayAgainHandler(IGrainFactory grains) : IMessageHandler<Game2PlayAgainMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2PlayAgainMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await SnowWarGames.ForPlayerAsync(grains, player.Id, game => game.PlayAgainAsync(player.Id));
        return true;
    }
}

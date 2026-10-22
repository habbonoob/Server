using Habbo.Protocol.Messages.Incoming.Game.Arena;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Game.Arena;

/// <summary>1930 Game2LoadStageReady — the arena has loaded this far.</summary>
internal sealed class Game2LoadStageReadyHandler(IGrainFactory grains) : IMessageHandler<Game2LoadStageReadyMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2LoadStageReadyMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await SnowWarGames.ForPlayerAsync(grains, player.Id, game => game.LoadedAsync(player.Id, message.Percentage));
        return true;
    }
}

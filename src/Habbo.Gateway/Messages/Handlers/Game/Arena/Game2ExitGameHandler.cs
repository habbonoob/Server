using Habbo.Protocol.Messages.Incoming.Game.Arena;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Game.Arena;

/// <summary>3330 Game2ExitGame — leave the arena.</summary>
internal sealed class Game2ExitGameHandler(IGrainFactory grains) : IMessageHandler<Game2ExitGameMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2ExitGameMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await SnowWarGames.ForPlayerAsync(grains, player.Id, game => game.ExitAsync(player.Id));
        return true;
    }
}

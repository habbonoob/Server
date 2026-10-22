using Habbo.Protocol.Messages.Incoming.Game.Directory;
using Habbo.Silo.Abstractions.Games;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Game.Directory;

/// <summary>3573 Game2StartSnowWar — start a game on a named arena.</summary>
internal sealed class Game2StartSnowWarHandler(IGrainFactory grains) : IMessageHandler<Game2StartSnowWarMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2StartSnowWarMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await grains.GetGrain<ISnowWarDirectoryGrain>(ISnowWarDirectoryGrain.Key).StartAsync(player, message.LevelName);
        return true;
    }
}

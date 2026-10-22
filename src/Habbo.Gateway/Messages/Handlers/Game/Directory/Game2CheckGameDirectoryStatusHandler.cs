using Habbo.Protocol.Messages.Incoming.Game.Directory;
using Habbo.Silo.Abstractions.Games;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Game.Directory;

/// <summary>1785 Game2CheckGameDirectoryStatus — whether the game directory is open.</summary>
internal sealed class Game2CheckGameDirectoryStatusHandler(IGrainFactory grains) : IMessageHandler<Game2CheckGameDirectoryStatusMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2CheckGameDirectoryStatusMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await grains.GetGrain<ISnowWarDirectoryGrain>(ISnowWarDirectoryGrain.Key).SendStatusAsync(player);
        return true;
    }
}

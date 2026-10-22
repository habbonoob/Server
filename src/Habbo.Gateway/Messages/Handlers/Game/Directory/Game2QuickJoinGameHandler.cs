using Habbo.Protocol.Messages.Incoming.Game.Directory;
using Habbo.Silo.Abstractions.Games;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Game.Directory;

/// <summary>2816 Game2QuickJoinGame — join a game waiting for players, or start one.</summary>
internal sealed class Game2QuickJoinGameHandler(IGrainFactory grains) : IMessageHandler<Game2QuickJoinGameMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2QuickJoinGameMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await grains.GetGrain<ISnowWarDirectoryGrain>(ISnowWarDirectoryGrain.Key).QuickJoinAsync(player);
        return true;
    }
}

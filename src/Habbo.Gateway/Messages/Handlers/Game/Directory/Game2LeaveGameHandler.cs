using Habbo.Protocol.Messages.Incoming.Game.Directory;
using Habbo.Silo.Abstractions.Games;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Game.Directory;

/// <summary>2648 Game2LeaveGame — stop waiting for a game.</summary>
internal sealed class Game2LeaveGameHandler(IGrainFactory grains) : IMessageHandler<Game2LeaveGameMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2LeaveGameMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await grains.GetGrain<ISnowWarDirectoryGrain>(ISnowWarDirectoryGrain.Key).LeaveAsync(player.Id);
        return true;
    }
}

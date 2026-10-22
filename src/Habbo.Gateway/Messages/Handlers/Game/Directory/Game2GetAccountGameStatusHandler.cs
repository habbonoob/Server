using Habbo.Protocol.Messages.Incoming.Game.Directory;
using Habbo.Silo.Abstractions.Games;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Game.Directory;

/// <summary>1148 Game2GetAccountGameStatus — the free games the player has left.</summary>
internal sealed class Game2GetAccountGameStatusHandler(IGrainFactory grains) : IMessageHandler<Game2GetAccountGameStatusMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2GetAccountGameStatusMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await grains.GetGrain<ISnowWarDirectoryGrain>(ISnowWarDirectoryGrain.Key).SendAccountStatusAsync(player, message.GameTypeId);
        return true;
    }
}

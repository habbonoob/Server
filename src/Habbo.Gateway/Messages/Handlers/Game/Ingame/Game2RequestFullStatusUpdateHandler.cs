using Habbo.Protocol.Messages.Incoming.Game.Ingame;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Game.Ingame;

/// <summary>668 Game2RequestFullStatusUpdate — the client lost step and wants the whole game.</summary>
internal sealed class Game2RequestFullStatusUpdateHandler(IGrainFactory grains) : IMessageHandler<Game2RequestFullStatusUpdateMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2RequestFullStatusUpdateMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await SnowWarGames.ForPlayerAsync(grains, player.Id, game => game.RequestFullStatusAsync(player.Id));
        return true;
    }
}

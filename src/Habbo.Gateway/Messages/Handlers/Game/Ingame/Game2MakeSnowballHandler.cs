using Habbo.Protocol.Messages.Incoming.Game.Ingame;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Game.Ingame;

/// <summary>2613 Game2MakeSnowball — make a snowball.</summary>
internal sealed class Game2MakeSnowballHandler(IGrainFactory grains) : IMessageHandler<Game2MakeSnowballMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2MakeSnowballMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await SnowWarGames.ForPlayerAsync(grains, player.Id, game => game.MakeSnowballAsync(player.Id));
        return true;
    }
}

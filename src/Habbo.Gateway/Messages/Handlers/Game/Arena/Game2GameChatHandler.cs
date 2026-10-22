using Habbo.Protocol.Messages.Incoming.Game.Arena;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Game.Arena;

/// <summary>2361 Game2GameChat — say something in the arena.</summary>
internal sealed class Game2GameChatHandler(IGrainFactory grains) : IMessageHandler<Game2GameChatMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2GameChatMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;
        // a staff mute holds in the arena as in rooms
        if (player.MutedUntil > DateTimeOffset.UtcNow)
            return true;

        await SnowWarGames.ForPlayerAsync(grains, player.Id, game => game.ChatAsync(player.Id, message.Message));
        return true;
    }
}

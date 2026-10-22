using Habbo.Protocol.Messages.Incoming.Game.Score;
using Habbo.Silo.Abstractions.Games;

namespace Habbo.Gateway.Messages.Handlers.Game.Score;

/// <summary>3273 Game2GetFriendsLeaderboard — the friends' all-time leaderboard.</summary>
internal sealed class Game2GetFriendsLeaderboardHandler(ISnowWarLeaderboards leaderboards) : IMessageHandler<Game2GetFriendsLeaderboardEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2GetFriendsLeaderboardEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await leaderboards.SendFriendsAsync(player, new LeaderboardQuery(message.GameTypeId, 0, message.StartRank, message.WindowSize));
        return true;
    }
}

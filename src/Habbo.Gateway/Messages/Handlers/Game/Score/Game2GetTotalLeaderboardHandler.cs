using Habbo.Protocol.Messages.Incoming.Game.Score;
using Habbo.Silo.Abstractions.Games;

namespace Habbo.Gateway.Messages.Handlers.Game.Score;

/// <summary>2437 Game2GetTotalLeaderboard — the all-time leaderboard.</summary>
internal sealed class Game2GetTotalLeaderboardHandler(ISnowWarLeaderboards leaderboards) : IMessageHandler<Game2GetTotalLeaderboardEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2GetTotalLeaderboardEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await leaderboards.SendTotalAsync(player, new LeaderboardQuery(message.GameTypeId, 0, message.StartRank, message.WindowSize));
        return true;
    }
}

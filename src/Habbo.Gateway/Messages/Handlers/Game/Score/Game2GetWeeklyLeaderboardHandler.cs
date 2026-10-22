using Habbo.Protocol.Messages.Incoming.Game.Score;
using Habbo.Silo.Abstractions.Games;

namespace Habbo.Gateway.Messages.Handlers.Game.Score;

/// <summary>1356 Game2GetWeeklyLeaderboard — a week's leaderboard.</summary>
internal sealed class Game2GetWeeklyLeaderboardHandler(ISnowWarLeaderboards leaderboards) : IMessageHandler<Game2GetWeeklyLeaderboardEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2GetWeeklyLeaderboardEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await leaderboards.SendWeeklyAsync(player, new LeaderboardQuery(message.GameTypeId, message.WeekOffset, message.StartRank, message.WindowSize));
        return true;
    }
}

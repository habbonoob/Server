using Habbo.Protocol.Messages.Incoming.Game.Score;
using Habbo.Silo.Abstractions.Games;

namespace Habbo.Gateway.Messages.Handlers.Game.Score;

/// <summary>3101 Game2GetWeeklyGroupLeaderboard — a week's groups' leaderboard.</summary>
internal sealed class Game2GetWeeklyGroupLeaderboardHandler(ISnowWarLeaderboards leaderboards) : IMessageHandler<Game2GetWeeklyGroupLeaderboardEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2GetWeeklyGroupLeaderboardEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await leaderboards.SendWeeklyGroupsAsync(player, new LeaderboardQuery(message.GameTypeId, message.WeekOffset, message.StartRank, message.WindowSize));
        return true;
    }
}

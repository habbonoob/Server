using Habbo.Protocol.Messages.Incoming.Game.Score;
using Habbo.Silo.Abstractions.Games;

namespace Habbo.Gateway.Messages.Handlers.Game.Score;

/// <summary>3025 Game2GetWeeklyFriendsLeaderboard — a week's friends' leaderboard.</summary>
internal sealed class Game2GetWeeklyFriendsLeaderboardHandler(ISnowWarLeaderboards leaderboards) : IMessageHandler<Game2GetWeeklyFriendsLeaderboardEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2GetWeeklyFriendsLeaderboardEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await leaderboards.SendWeeklyFriendsAsync(player, new LeaderboardQuery(message.GameTypeId, message.WeekOffset, message.StartRank, message.WindowSize));
        return true;
    }
}

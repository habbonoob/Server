using Habbo.Protocol.Messages.Incoming.Game.Score;
using Habbo.Silo.Abstractions.Games;

namespace Habbo.Gateway.Messages.Handlers.Game.Score;

/// <summary>963 Game2GetTotalGroupLeaderboard — the groups' all-time leaderboard.</summary>
internal sealed class Game2GetTotalGroupLeaderboardHandler(ISnowWarLeaderboards leaderboards) : IMessageHandler<Game2GetTotalGroupLeaderboardEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, Game2GetTotalGroupLeaderboardEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await leaderboards.SendGroupsAsync(player, new LeaderboardQuery(message.GameTypeId, 0, message.StartRank, message.WindowSize));
        return true;
    }
}

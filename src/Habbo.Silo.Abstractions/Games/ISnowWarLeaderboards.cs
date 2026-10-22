using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Games;

/// <summary>The game center's leaderboards: everyone, the player and their friends, or the groups (each player's
/// points counting for their favourite group), of all time or of a week. Every call answers the player.</summary>
public interface ISnowWarLeaderboards
{
    Task SendTotalAsync(PlayerData player, LeaderboardQuery query);

    Task SendFriendsAsync(PlayerData player, LeaderboardQuery query);

    Task SendGroupsAsync(PlayerData player, LeaderboardQuery query);

    Task SendWeeklyAsync(PlayerData player, LeaderboardQuery query);

    Task SendWeeklyFriendsAsync(PlayerData player, LeaderboardQuery query);

    Task SendWeeklyGroupsAsync(PlayerData player, LeaderboardQuery query);
}

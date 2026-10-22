using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Achievements;

/// <summary>Reads how far a player has got with the achievements they have started.</summary>
public interface IAchievementService
{
    Task<IReadOnlyList<AchievementProgressView>> GetProgressAsync(Guid playerId, CancellationToken ct = default);

    /// <summary>Every achievement the hotel has, with this player's progress through each and
    /// the ones they have not started shown at their first level. The client builds its whole
    /// dialog from this, so anything left out is invisible to the player.</summary>
    Task<IReadOnlyList<AchievementProgressView>> GetAllAsync(Guid playerId, CancellationToken ct = default);

    /// <summary>Where the player stands on one achievement, started or not.</summary>
    Task<AchievementProgressView?> GetProgressAsync(Guid playerId, int achievementId, CancellationToken ct = default);

    /// <summary>What the player's achievements add up to: the reward points of every level they
    /// have finished. Worked out from the levels rather than kept as a running total, so it can
    /// never drift from the achievements it is meant to describe.</summary>
    Task<int> GetScoreAsync(Guid playerId, CancellationToken ct = default);
}

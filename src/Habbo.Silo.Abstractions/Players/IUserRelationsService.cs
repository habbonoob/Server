using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Players;

/// <summary>The lists of people a player has shut out. The client loads them at login and reads
/// them everywhere — room chat, the console, friend requests — rather than only in the friend
/// list, which is why they are neither the friend list's nor the console's.</summary>
public interface IUserRelationsService
{
    /// <summary>The account ids the player has muted.</summary>
    Task<IReadOnlyList<int>> GetIgnoredAsync(Guid playerId, CancellationToken ct = default);

    /// <summary>The account ids the player has blocked, which is a different list from muted.
    /// </summary>
    Task<IReadOnlyList<int>> GetBlockedAsync(Guid playerId, CancellationToken ct = default);
}

using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Friends;

/// <summary>The player's friend list: who they are friends with, the groups they file them
/// under, and who is waiting on an answer. Messaging is a separate thing — see
/// <see cref="IMessengerService"/> — because the client keeps them apart too.</summary>
public interface IFriendListService
{
    Task<FriendListView> GetFriendListAsync(Guid playerId, CancellationToken ct = default);

    Task<IReadOnlyList<FriendRequestView>> GetFriendRequestsAsync(Guid playerId, CancellationToken ct = default);

    /// <summary>Drops friends, from both sides. Returns the ones that were actually there, so
    /// the caller only tells the clients about real removals.</summary>
    Task<IReadOnlyList<FriendRemoval>> RemoveFriendsAsync(
        Guid playerId, IReadOnlyList<int> friendAccountIds, CancellationToken ct = default);

    /// <summary>Asks somebody to be friends. Refused when they are already friends, when a
    /// request is already pending, or when the target has blocked the asker.</summary>
    Task<FriendRequestResult> RequestFriendAsync(
        Guid playerId, string username, CancellationToken ct = default);

    /// <summary>Accepts pending requests, which makes friends of both sides.</summary>
    Task<FriendAcceptance> AcceptFriendsAsync(
        Guid playerId, IReadOnlyList<int> requesterAccountIds, CancellationToken ct = default);

    /// <summary>Turns down requests, or all of them.</summary>
    Task DeclineFriendsAsync(
        Guid playerId, bool declineAll, IReadOnlyList<int> requesterAccountIds, CancellationToken ct = default);

    /// <summary>Tells each friend who is online how the player looks in their list now: online or not, in a room or
    /// not, under the category and with the relationship that friend gave them. Their lists have no other way of
    /// finding out between the client's own refreshes, a quarter of an hour apart. Friends the player hides being
    /// online from are told they are offline.</summary>
    Task AnnouncePresenceAsync(Guid playerId, bool online, CancellationToken ct = default);
}

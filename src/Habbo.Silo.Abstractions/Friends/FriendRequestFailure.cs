using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Friends;

/// <summary>Why a friend request did not go through, or that it did.</summary>
public enum FriendRequestFailure
{
    None = 0,
    UnknownUser,
    Themselves,
    AlreadyFriends,
    AlreadyAsked,
    Blocked,
    TargetListFull,

    /// <summary>The player blocked the one they are asking.</summary>
    BlockedByRequester,

    /// <summary>The player's own list is full.</summary>
    OwnListFull,

    /// <summary>The one asked takes no friend requests.</summary>
    RequestsDisabled,
}

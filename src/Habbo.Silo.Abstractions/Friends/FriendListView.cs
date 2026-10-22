using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Friends;

/// <summary>The friend list as the client sets it up: what the player may keep, their own
/// groups, and the friends themselves.</summary>
[GenerateSerializer]
public partial record FriendListView(
    /// <summary>What this player may keep, which is one of the two below.</summary>
    [property: Id(0)] int FriendLimit,
    /// <summary>What an ordinary player may keep, and what a club member may — the client shows
    /// both so a player can see what membership would buy.</summary>
    [property: Id(1)] int NormalFriendLimit,
    [property: Id(2)] int ExtendedFriendLimit,
    [property: Id(3)] IReadOnlyList<FriendCategoryView> Categories,
    [property: Id(4)] IReadOnlyList<FriendView> Friends);

namespace Habbo.Silo.Abstractions.Friends;

/// <summary>A friend request that could not be accepted, and why.</summary>
[GenerateSerializer]
public sealed partial record FriendAcceptFailure(
    [property: Id(0)] int RequesterAccountId, [property: Id(1)] FriendRequestFailure Reason);

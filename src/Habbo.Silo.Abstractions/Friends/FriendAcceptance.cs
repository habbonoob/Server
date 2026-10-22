namespace Habbo.Silo.Abstractions.Friends;

/// <summary>What accepting friend requests did: the friendships made, and the requests that could
/// not be accepted, with why.</summary>
[GenerateSerializer]
public sealed partial record FriendAcceptance(
    [property: Id(0)] IReadOnlyList<FriendshipMade> Made,
    [property: Id(1)] IReadOnlyList<FriendAcceptFailure> Failures);

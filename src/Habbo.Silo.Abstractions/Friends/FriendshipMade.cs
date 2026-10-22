using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Friends;

/// <summary>A friendship that was just made, as each side needs to see the other.</summary>
[GenerateSerializer]
public partial record FriendshipMade(
    [property: Id(0)] Guid RequesterId,
    [property: Id(1)] FriendView ForPlayer,
    [property: Id(2)] FriendView ForRequester);

using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Friends;

/// <summary>A friendship that was dropped: both sides, so each client can be told.</summary>
[GenerateSerializer]
public partial record FriendRemoval(
    [property: Id(0)] Guid FriendId,
    [property: Id(1)] int FriendAccountId);

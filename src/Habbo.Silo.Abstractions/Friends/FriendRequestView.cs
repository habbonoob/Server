using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Friends;

[GenerateSerializer]
public partial record FriendRequestView(
    [property: Id(0)] int RequesterAccountId,
    [property: Id(1)] string RequesterName,
    [property: Id(2)] string Figure);

using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Friends;

[GenerateSerializer]
public partial record FriendRequestResult(
    [property: Id(0)] FriendRequestFailure Failure,
    [property: Id(1)] Guid TargetId,
    [property: Id(2)] FriendRequestView? Request)
{
    public bool Sent => Failure == FriendRequestFailure.None;
}

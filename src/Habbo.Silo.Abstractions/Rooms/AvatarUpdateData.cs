using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>One avatar state change in a UserUpdate batch.</summary>
[GenerateSerializer]
public partial record AvatarUpdateData(
    [property: Id(0)] int RoomIndex,
    [property: Id(1)] int X,
    [property: Id(2)] int Y,
    [property: Id(3)] string Z,
    [property: Id(4)] int HeadDirection,
    [property: Id(5)] int BodyDirection,
    [property: Id(6)] string Status);

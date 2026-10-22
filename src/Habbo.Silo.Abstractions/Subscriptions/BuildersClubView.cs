using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Subscriptions;

/// <summary>How long a Builders Club membership has left and what it allows.</summary>
[GenerateSerializer]
public partial record BuildersClubView(
    [property: Id(0)] int SecondsLeft,
    [property: Id(1)] int FurniLimit,
    [property: Id(2)] int MaxFurniLimit,
    [property: Id(3)] int SecondsLeftWithGrace);

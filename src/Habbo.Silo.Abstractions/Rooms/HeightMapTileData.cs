using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>A tile whose height or stacking-blocked flag changed.</summary>
[GenerateSerializer]
public partial record HeightMapTileData(
    [property: Id(0)] int X,
    [property: Id(1)] int Y,
    [property: Id(2)] double Height,
    [property: Id(3)] bool StackingBlocked);

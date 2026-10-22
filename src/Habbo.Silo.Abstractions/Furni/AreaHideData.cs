using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Furni;

/// <summary>A furni-driven hidden floor area (FloorHeightMap trailer).</summary>
[GenerateSerializer]
public partial record AreaHideData(
    [property: Id(0)] int FurniId,
    [property: Id(1)] bool On,
    [property: Id(2)] int RootX,
    [property: Id(3)] int RootY,
    [property: Id(4)] int Width,
    [property: Id(5)] int Length,
    [property: Id(6)] bool Invert);

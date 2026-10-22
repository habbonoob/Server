using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>Room visualization + geometry settings the client reads on entry.</summary>
[GenerateSerializer]
public partial record RoomVisualizationData(
    [property: Id(0)] string FloorType,
    [property: Id(1)] string Wallpaper,
    [property: Id(2)] string Landscape,
    [property: Id(3)] bool WallsHidden,
    [property: Id(4)] int WallThicknessExponent,
    [property: Id(5)] int FloorThicknessExponent,
    [property: Id(6)] bool UseSmallScale,
    [property: Id(7)] int FixedWallsHeight,
    [property: Id(8)] int CameraInitX,
    [property: Id(9)] int CameraInitY,
    [property: Id(10)] double CameraInitZ);

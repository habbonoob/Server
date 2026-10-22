using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>1580 FloorHeightMapMessageComposer — the room-render trigger. Must arrive AFTER
/// HeightMap: the client's handler bails silently without the stacking map.</summary>
[Message(ServerMessages.FloorHeightMapMessageComposer)]
public sealed partial record FloorHeightMapMessageComposer(
    bool UseSmallScale,
    int FixedWallsHeight,
    string Heightmap,
    IReadOnlyList<AreaHideWireData> AreaHides,
    int CameraInitX,
    int CameraInitY,
    float CameraInitZ)
{
    public static FloorHeightMapMessageComposer From(
        string heightmap, RoomVisualizationData visualization, IReadOnlyList<AreaHideData> areaHides) => new(
        visualization.UseSmallScale,
        visualization.FixedWallsHeight,
        heightmap,
        areaHides
            .Select(area => new AreaHideWireData(
                area.FurniId, area.On, area.RootX, area.RootY, area.Width, area.Length, area.Invert))
            .ToList(),
        visualization.CameraInitX,
        visualization.CameraInitY,
        (float)visualization.CameraInitZ);
}

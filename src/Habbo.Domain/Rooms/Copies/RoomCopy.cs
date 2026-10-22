namespace Habbo.Domain.Rooms.Copies;

/// <summary>A room as another hotel showed it to its visitor, written down to be built again here: its floor plan and
/// door, its walls and floor, and every furni with where it stands and what it shows. Recorded by the room copier
/// (tools/Habbo.RoomCopier), a G-Earth extension, from what the client was sent; read by the room import.</summary>
/// <param name="Format">The layout of this record, <see cref="CurrentFormat"/> when written.</param>
/// <param name="Hotel">Where it was copied from (com, fi, ...).</param>
/// <param name="Heightmap">The floor plan as the client was sent it: a row per line, separated by '\r'.</param>
/// <param name="FixedWallsHeight">The wall height the room was given, -1 when the client works it out itself.</param>
/// <param name="WallThickness">Habbo's thickness steps: -2 thinnest, -1 thin, 0 normal, 1 thick.</param>
/// <param name="Variables">The room's wired variables, when its wired was copied (format 3).</param>
public sealed record RoomCopy(
    int Format,
    string Hotel,
    long SourceRoomId,
    string Name,
    string Description,
    DateTimeOffset CopiedAt,
    string Heightmap,
    int DoorX,
    int DoorY,
    int DoorDirection,
    int FixedWallsHeight,
    bool UseSmallScale,
    int WallThickness,
    int FloorThickness,
    bool WallsHidden,
    string Wallpaper,
    string Floor,
    string Landscape,
    IReadOnlyList<RoomCopyFloorItem> FloorItems,
    IReadOnlyList<RoomCopyWallItem> WallItems,
    IReadOnlyList<RoomCopyVariable>? Variables = null)
{
    /// <summary>2 added the items' own ids and the wired boxes' settings, 3 the room's variables; an older copy reads
    /// the same, without them.</summary>
    public const int CurrentFormat = 3;
}

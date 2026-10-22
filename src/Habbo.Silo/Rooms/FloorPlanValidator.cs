using Habbo.Protocol.Rooms;

namespace Habbo.Silo.Rooms;

/// <summary>Checks a floor plan from the editor the way the client's texts describe its failures
/// ("notification.floorplan_editor.error.message.&lt;key&gt;"), and tidies it into a heightmap.</summary>
public static class FloorPlanValidator
{
    // the failures, by the key the client words them with
    public const string TooLargeWidth = "too_large_width";
    public const string TooLargeHeight = "too_large_height";
    public const string TooLargeArea = "too_large_area";
    public const string EffectiveWidthIsZero = "effective_width_is_0";
    public const string EffectiveHeightIsZero = "effective_height_is_0";
    public const string EntryTileOutsideMap = "entry_tile_outside_map";
    public const string EntryNotOnTile = "entry_not_on_tile";
    public const string InvalidEntryTileDirection = "invalid_entry_tile_direction";
    public const string InvalidDoorSetup = "invalid_door_setup";
    public const string InvalidWallThickness = "invalid_wall_thickness";
    public const string InvalidFloorThickness = "invalid_floor_thickness";
    public const string InvalidWallsFixedHeight = "invalid_walls_fixed_height";
    public const string ChangeBlockedByRoomItem = "change_blocked_by_room_item";
    public const string General = "general";

    private const char Void = 'x';

    /// <summary>The tile heights the editor paints with, lowest first.</summary>
    private const string Heights = "0123456789abcdefghijklmnopqrstuvwxyz";

    /// <summary>The thickness settings the editor offers: a quarter, a half, normal and double.</summary>
    private const int MinThickness = -2;
    private const int MaxThickness = 1;

    /// <summary>What the walls' fixed height is when the client derives it from the floor.</summary>
    public const int DerivedWallHeight = -1;

    private const int DirectionCount = 8;

    /// <summary>The problems with the plan; none when it may be saved. <paramref name="kept"/> are the
    /// tiles furniture stands on, which must stay floor at the height they are.</summary>
    public static List<string> Validate(FloorPlan plan, RoomOptions options, bool largePlansAllowed,
        IEnumerable<(int X, int Y)> kept, RoomModel current, out string heightmap)
    {
        var errors = new List<string>();
        var rows = plan.Heightmap.Replace("\r\n", "\r").Replace('\n', '\r')
            .Split('\r', StringSplitOptions.RemoveEmptyEntries)
            .Select(row => row.ToLowerInvariant())
            .ToList();
        var width = rows.Count == 0 ? 0 : rows.Max(row => row.Length);
        var height = rows.Count;
        rows = rows.Select(row => row.PadRight(width, Void)).ToList();
        heightmap = string.Join('\r', rows);

        if (rows.Any(row => row.Any(cell => cell != Void && !Heights.Contains(cell))))
            errors.Add(General);
        if (width > options.FloorPlanMaxAxis)
            errors.Add(TooLargeWidth);
        if (height > options.FloorPlanMaxAxis)
            errors.Add(TooLargeHeight);
        if (!largePlansAllowed && (width - 1) * (height - 1) > options.FloorPlanMaxArea)
            errors.Add(TooLargeArea);

        bool IsTile(int x, int y) => y >= 0 && y < height && x >= 0 && x < width && rows[y][x] != Void;

        if (!Enumerable.Range(0, width).Any(x => Enumerable.Range(0, height).Any(y => IsTile(x, y))))
        {
            errors.Add(EffectiveWidthIsZero);
            errors.Add(EffectiveHeightIsZero);
        }

        if (plan.DoorX < 0 || plan.DoorY < 0 || plan.DoorX >= width || plan.DoorY >= height)
            errors.Add(EntryTileOutsideMap);
        else if (!IsTile(plan.DoorX, plan.DoorY))
            errors.Add(EntryNotOnTile);

        if (plan.DoorDirection < 0 || plan.DoorDirection >= DirectionCount)
            errors.Add(InvalidEntryTileDirection);

        // the first row and column hold at most the door, which the walls leave a gap for
        var edgeTiles = Enumerable.Range(0, width).Where(x => IsTile(x, 0)).Select(x => (x, 0))
            .Concat(Enumerable.Range(1, Math.Max(0, height - 1)).Where(y => IsTile(0, y)).Select(y => (0, y)))
            .ToList();
        if (edgeTiles.Count > 1 || (edgeTiles.Count == 1 && edgeTiles[0] != (plan.DoorX, plan.DoorY)))
            errors.Add(InvalidDoorSetup);

        if (plan.WallThickness is < MinThickness or > MaxThickness)
            errors.Add(InvalidWallThickness);
        if (plan.FloorThickness is < MinThickness or > MaxThickness)
            errors.Add(InvalidFloorThickness);
        if (plan.FixedWallsHeight != DerivedWallHeight && (plan.FixedWallsHeight < 0 || plan.FixedWallsHeight > options.FloorPlanMaxWallHeight))
            errors.Add(InvalidWallsFixedHeight);

        foreach (var (x, y) in kept)
        {
            if (!IsTile(x, y) || Heights.IndexOf(rows[y][x]) != (int)current.GetHeight(x, y))
            {
                errors.Add(ChangeBlockedByRoomItem);
                break;
            }
        }

        return errors;
    }
}

using System.Globalization;
using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>What wired reads from furni: the states an item can show, where it stands, and whether
/// it still looks the way a snapshot recorded it.</summary>
public static class WiredFurni
{
    /// <summary>The states the item's artwork can show, in order. Negative states are transient
    /// looks, such as a dice rolling, which nothing should land on.</summary>
    public static List<int> States(RoomItemStore.PlacedItem item) =>
        item.Item.Definition!.States.Select(s => s.State).Where(state => state >= 0).Order().ToList();

    /// <summary>Whether wired can change the item's state: a plain state it can step through.</summary>
    public static bool HasStates(RoomItemStore.PlacedItem item) =>
        item.Data.Type == FurniDataView.LegacyType && States(item).Count >= 2;

    public static int CurrentState(RoomItemStore.PlacedItem item) =>
        int.TryParse(item.Data.State, NumberStyles.Integer, CultureInfo.InvariantCulture, out var state) ? state : 0;

    public static string StateText(int state) => state.ToString(CultureInfo.InvariantCulture);

    /// <summary>The tile one step from (x, y) in a wire direction: 0 is towards -y, then clockwise.</summary>
    public static (int X, int Y) Step(int x, int y, int direction) => (direction % 8) switch
    {
        0 => (x, y - 1),
        1 => (x + 1, y - 1),
        2 => (x + 1, y),
        3 => (x + 1, y + 1),
        4 => (x, y + 1),
        5 => (x - 1, y + 1),
        6 => (x - 1, y),
        _ => (x - 1, y - 1),
    };

    /// <summary>Whether the item is a wired antenna, which signals go through.</summary>
    public static bool IsClickTile(RoomItemStore.PlacedItem item) =>
        item.Item.Definition!.InteractionName == Items.Interactions.ClickTileInteraction.Name;

    public static bool IsAntenna(RoomItemStore.PlacedItem item) =>
        item.Item.Definition!.InteractionName == Items.Interactions.WiredAntennaInteraction.Name;

    // movement, as the move dialogs number it: none, a random direction, left or right, up or down,
    // then the four straight and the four diagonal directions
    private const int RandomDirection = 1;
    private const int LeftOrRight = 2;
    private const int UpOrDown = 3;
    private const int FirstStraight = 4;
    private const int FirstDiagonal = 8;
    private const int DirectionsEachWay = 4;
    private const int QuarterTurn = 2;
    private const int Directions = 8;

    /// <summary>The wire direction a move dialog's movement choice means; null for "no movement".</summary>
    public static int? MoveDirection(int movement, Random random) => movement switch
    {
        RandomDirection => random.Next(DirectionsEachWay) * QuarterTurn,
        LeftOrRight => random.Next(2) == 0 ? 2 : 6,
        UpOrDown => random.Next(2) == 0 ? 0 : 4,
        >= FirstStraight and < FirstStraight + DirectionsEachWay => (movement - FirstStraight) * QuarterTurn,
        >= FirstDiagonal and < FirstDiagonal + DirectionsEachWay => (movement - FirstDiagonal) * QuarterTurn + 1,
        _ => null,
    };

    // rotation: none, clockwise, anticlockwise, random
    private const int Clockwise = 1;
    private const int Anticlockwise = 2;
    private const int RandomRotation = 3;

    /// <summary>A furni's rotation turned as the rotate dialog chooses, a quarter turn at a time.</summary>
    public static int Rotate(int direction, int rotation, Random random) => rotation switch
    {
        Clockwise => (direction + QuarterTurn) % Directions,
        Anticlockwise => (direction + Directions - QuarterTurn) % Directions,
        RandomRotation => random.Next(Directions / QuarterTurn) * QuarterTurn,
        _ => direction,
    };

    /// <summary>Whether a user stands on one of the item's tiles.</summary>
    public static bool IsUnder(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item) =>
        RoomItemStore.Footprint(item).Any(tile => tile == (user.X, user.Y));

    /// <summary>Which parts of a snapshot a match looks at, from the dialog's checkboxes.</summary>
    public readonly record struct SnapshotParts(bool State, bool Direction, bool Position, bool Altitude)
    {
        public static SnapshotParts Of(WiredBox box) => new(box.Flag(0), box.Flag(1), box.Flag(2), box.Flag(3));
    }

    /// <summary>Whether the item still has what the snapshot recorded, in the parts that count.</summary>
    public static bool Matches(RoomItemStore.PlacedItem item, FurniWiredSnapshot snapshot, SnapshotParts parts) =>
        (!parts.State || !HasStates(item) || item.Data.StateText == snapshot.State)
        && (!parts.Direction || item.Rotation == snapshot.Rotation)
        && (!parts.Position || (item.X == snapshot.X && item.Y == snapshot.Y))
        && (!parts.Altitude || Math.Abs(item.Z - snapshot.Z) < AltitudeTolerance);

    /// <summary>How close two altitudes must be to count as the same: the hundredth the dialogs work in.</summary>
    public const double AltitudeTolerance = 0.005;
}

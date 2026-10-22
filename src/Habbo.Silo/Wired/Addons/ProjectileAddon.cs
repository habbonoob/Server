using Habbo.Silo.Rooms;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Addons;

/// <summary>Moves its stack's projectiles (by default whatever the stack moves) as projectiles: turned to face where they
/// fly, over a time per tile of the distance, overshooting where they stop, along a curve.
/// <para>What the client draws comes from its wired movement message (September 2026): the time, an overshoot in
/// milliseconds and a curve strength. The rest is inferred from the dialog: the four direction systems are all read as
/// the nearest of eight (or four) straight directions, the speed-up for long moves, the shooter's turn and hop and the
/// projectile's own animation variables are not done; none of it was measured.</para></summary>
[WiredBox("wf_xtra_rotate_to_dir")]
public sealed class ProjectileAddon : WiredAddon
{
    // the dialog's fields, in its order
    private const int RotateToDirection = 0;
    private const int DirectionalSystem = 1;
    private const int TimeScalesWithDistance = 2;
    private const int TimeOperand = 3;
    private const int TimePerTile = 4;
    private const int TimeTarget = 5;
    private const int CountX = 6;
    private const int CountY = 7;
    private const int CountHeight = 8;
    private const int RotationOffset = 10;
    private const int DistanceMode = 14;
    private const int DistanceOperand = 15;
    private const int Distance = 16;
    private const int DistanceTarget = 17;
    private const int Curve = 18;

    private const int FourDirectionsFirst = 2;
    private const int Overshoot = 1;
    private const int AlwaysShoot = 2;
    private const int FromVariable = 1;
    private const int MaxTimePerTile = 100_000;

    // the variables: time per tile, then distance; each read from its merged holder slot (furni, users)
    private const int TimeVariable = 0;
    private const int DistanceVariable = 1;

    private const int Projectiles = 0;

    public override int Code => AddonCodes.Projectile;
    public override IReadOnlyList<WiredSlot> FurniSlots =>
    [
        new([WiredSources.TriggerFurni, WiredSources.PickedFurni, WiredSources.SelectedFurni, WiredSources.SignalFurni, WiredSources.MovedFurni],
            WiredSources.MovedFurni),
        WiredSlot.Furni.DefaultingTo(WiredSources.TriggerFurni),
        WiredSlot.Furni.DefaultingTo(WiredSources.TriggerFurni),
    ];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users, WiredSlot.Users, WiredSlot.Users];
    public override IReadOnlyList<int> DefaultIntParams => [1, 1, 0, 0, 100, -20, 1, 1, 0, 4, 0, 0, 1, 1, 0, 0, 1, -20, 50];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    /// <summary>Whether the item flies as one of the box's projectiles: by default, anything its stack moves.</summary>
    public static bool Flies(WiredBox box, WiredRun run, RoomItemStore.PlacedItem item) =>
        box.FurniSource(Projectiles) == WiredSources.MovedFurni || run.Furni(box, Projectiles).Contains(item);

    /// <summary>The move of a projectile from where it stands to (x, y): its rotation, and the options it flies with.</summary>
    public static (int Rotation, WiredMoveOptions Options) Shape(WiredBox box, WiredRun run, RoomItemStore.PlacedItem item, int x, int y,
        int rotation, WiredMoveOptions options)
    {
        var (dx, dy) = (x - item.X, y - item.Y);
        if (box.Flag(RotateToDirection) && (dx, dy) != (0, 0))
        {
            var facing = (Facing(dx, dy, box.IntParam(DirectionalSystem) >= FourDirectionsFirst) + box.IntParam(RotationOffset)) % 8;
            if (item.Item.Definition!.AllowsDirection(facing))
                rotation = facing;
        }

        var tiles = Math.Sqrt((box.Flag(CountX) ? dx * dx : 0) + (box.Flag(CountY) ? dy * dy : 0));
        if (box.Flag(TimeScalesWithDistance) && Value(box, run, TimeOperand, TimePerTile, TimeVariable, TimeTarget, 1, 0) is { } perTile && tiles > 0)
            options = options with { AnimationMilliseconds = (int)Math.Round(Math.Clamp(perTile, 1, MaxTimePerTile) * tiles) };

        // the client takes the overshoot as time at the same speed: the extra tiles' share of the move's time
        var moved = Math.Max(Math.Abs(dx), Math.Abs(dy));
        if (moved > 0 && box.IntParam(DistanceMode) is Overshoot or AlwaysShoot
            && Value(box, run, DistanceOperand, Distance, DistanceVariable, DistanceTarget, 2, 2) is { } distance)
        {
            var extra = box.IntParam(DistanceMode) == Overshoot ? distance : distance - moved;
            if (extra > 0)
                options = options with { OvershootMilliseconds = options.AnimationMilliseconds * extra / moved };
        }

        // a movement curve add-on goes over the projectile's own
        if (options.CurveStrength is null && box.IntParam(Curve) != 0)
            options = options with { CurveStrength = box.IntParam(Curve) };
        return (rotation, options);
    }

    /// <summary>The nearest of the eight wire directions (0 is towards -y, then clockwise), or of the four straight ones.</summary>
    private static int Facing(int dx, int dy, bool fourDirections)
    {
        var angle = Math.Atan2(dx, -dy);
        return fourDirections
            ? ((int)Math.Round(angle / (Math.PI / 2)) % 4 + 4) % 4 * 2
            : ((int)Math.Round(angle / (Math.PI / 4)) % 8 + 8) % 8;
    }

    /// <summary>A setting given as a number or by a variable, read from the holders of its merged slot.</summary>
    private static int? Value(WiredBox box, WiredRun run, int operand, int number, int variable, int target, int furniSlot, int userSlot)
    {
        if (box.IntParam(operand) != FromVariable)
            return box.IntParam(number);
        if (WiredVariableUse.Variable(box, run.Room, variable) is not { } read)
            return null;

        IEnumerable<WiredHolder> holders = box.IntParam(target) switch
        {
            WiredVariableTargets.Furni => run.Furni(box, furniSlot).Select(WiredHolder.Of).ToList(),
            WiredVariableTargets.User => run.Users(box, userSlot).Select(WiredHolder.Of).ToList(),
            WiredVariableTargets.Global => [WiredHolder.Room],
            WiredVariableTargets.Context => [WiredHolder.Context],
            _ => [],
        };
        return holders.Select(holder => run.Room.VariableValue(read, holder, run)).FirstOrDefault(value => value is not null);
    }
}

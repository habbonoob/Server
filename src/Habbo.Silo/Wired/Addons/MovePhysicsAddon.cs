namespace Habbo.Silo.Wired.Addons;

/// <summary>Changes what may stop the stack's moves: any furniture at all, or none; users, or
/// not; and whether moved furni keep their altitude.</summary>
[WiredBox("wf_xtra_mov_physics")]
public sealed class MovePhysicsAddon : WiredAddon
{
    // the client's order: keep altitude, through furni, through users, blocked by furni
    private const int KeepAltitude = 0;
    private const int ThroughFurni = 1;
    private const int ThroughUsers = 2;
    private const int BlockedByFurni = 3;

    public override int Code => AddonCodes.MovePhysics;
    public override IReadOnlyList<int> DefaultIntParams => [1, 0, 0, 0];

    public static WiredMoveOptions Apply(WiredBox box, WiredMoveOptions options) => options with
    {
        BlockedByFurni = box.Flag(BlockedByFurni),
        KeepAltitude = box.Flag(KeepAltitude),
        ThroughFurni = box.Flag(ThroughFurni),
        AvatarsBlock = options.AvatarsBlock && !box.Flag(ThroughUsers),
    };
}

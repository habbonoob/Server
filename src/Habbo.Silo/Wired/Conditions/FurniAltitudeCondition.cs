namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when its furni stand lower than, at, or higher than the chosen altitude
/// (in hundredths, 0 to 80).</summary>
[WiredBox("wf_cnd_has_altitude")]
public sealed class FurniAltitudeCondition : WiredFurniCondition
{
    private const double Hundredths = 100.0;

    public override int Code => ConditionCodes.FurniHasAltitude;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<int> DefaultIntParams => [0, WiredComparison.LowerThan];

    public override bool Matches(Rooms.RoomItemStore.PlacedItem item, WiredBox box, WiredRun run) =>
        WiredComparison.Holds(box.IntParam(1), (int)Math.Round(item.Z * Hundredths), box.IntParam(0));
}

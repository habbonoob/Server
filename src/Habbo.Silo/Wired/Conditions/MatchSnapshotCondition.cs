namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when its furni still match the picture the box took of them, in the parts the dialog chose (state,
/// direction, position, altitude): all or any of them, as the quantifier says.</summary>
[WiredBox("wf_cnd_match_snapshot")]
public class MatchSnapshotCondition : WiredCondition
{
    public override int Code => ConditionCodes.StatesMatch;
    public override bool AllowsWallFurni => true;
    public override bool PicksFurni => true;
    public override bool TakesSnapshot => true;
    public override byte QuantifierType => WiredQuantifierTypes.Furni;
    public override IReadOnlyList<int> DefaultIntParams => [0, 0, 0, 0];

    public override bool Passes(WiredBox box, WiredRun run)
    {
        var parts = WiredFurni.SnapshotParts.Of(box);
        var snapshots = box.Settings.Snapshots.ToDictionary(snapshot => snapshot.ItemId);
        // what the source gives that the snapshot never saw has nothing to match
        return WiredQuantity.Passes(box, IsNegative, run.Furni(box).Where(item => snapshots.ContainsKey(item.Item.Id)),
            item => WiredFurni.Matches(item, snapshots[item.Item.Id], parts));
    }
}

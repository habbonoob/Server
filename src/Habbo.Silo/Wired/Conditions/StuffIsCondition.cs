namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when its furni (the one the stack ran for, by default) are of a kind among its reference furni (the
/// picks, by default): all or any of them, as the quantifier says. Habbo's dialog: furni to test, then furni to compare
/// with.</summary>
[WiredBox("wf_cnd_stuff_is")]
public class StuffIsCondition : WiredCondition
{
    private const int Tested = 0;
    private const int Reference = 1;

    public override int Code => ConditionCodes.StuffTypeMatches;
    public override bool AllowsWallFurni => true;
    public override byte QuantifierType => WiredQuantifierTypes.Furni;
    public override IReadOnlyList<WiredSlot> FurniSlots =>
        [WiredSlot.EitherPicks.DefaultingTo(WiredSources.TriggerFurni), WiredSlot.EitherPicks];

    public override bool Passes(WiredBox box, WiredRun run)
    {
        var kinds = run.Furni(box, Reference).Select(item => item.Item.DefinitionId).ToHashSet();
        return WiredQuantity.Passes(box, IsNegative, run.Furni(box, Tested), item => kinds.Contains(item.Item.DefinitionId));
    }
}

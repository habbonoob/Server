namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when nothing stands on top of the picked furni: on none of them, or not on some
/// one, as the dialog chooses.</summary>
[WiredBox("wf_cnd_not_furni_on")]
public sealed class HasNoFurniOnCondition : WiredCondition
{
    public override int Code => ConditionCodes.NotHasStackedFurnis;
    public override bool PicksFurni => true;
    // its radio: 0 any, 1 all (the client's requireall texts); a new box on Habbo: any
    private const int RequireAll = 1;
    private const int RequireAny = 0;

    public override IReadOnlyList<int> DefaultIntParams => [RequireAny];

    public override bool Passes(WiredBox box, WiredRun run) =>
        WiredQuantity.Holds(box.IntParam(0) == RequireAll, run.Furni(box),
            item => !HasFurniOnCondition.HasFurniOn(item, run.Room));
}

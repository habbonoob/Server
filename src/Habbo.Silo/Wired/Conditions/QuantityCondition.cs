namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when the number of its furni — or of its users, whichever source the dialog
/// was given — is lower than, equal to, or higher than the chosen amount (0 to 100).</summary>
[WiredBox("wf_cnd_slc_quantity")]
public sealed class QuantityCondition : WiredCondition
{
    private const int CountsUsers = 0;
    private const int Amount = 1;
    private const int Comparison = 2;

    public override int Code => ConditionCodes.InputSourceQuantity;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.Unpicked];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users.DefaultingTo(WiredSources.SelectedUsers)];
    public override IReadOnlyList<int> DefaultIntParams => [0, 5, WiredComparison.LowerThan];

    public override bool Passes(WiredBox box, WiredRun run)
    {
        var count = box.Flag(CountsUsers) ? run.Users(box).Count() : run.Furni(box).Count();
        return WiredComparison.Holds(box.IntParam(Comparison), count, box.IntParam(Amount));
    }
}

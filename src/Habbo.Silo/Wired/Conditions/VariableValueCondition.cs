using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when the chosen variable's value, for its holders, compares as chosen with a
/// number or another variable's value.</summary>
[WiredBox("wf_cnd_var_val_match")]
public sealed class VariableValueCondition : WiredCondition
{
    private const int Target = 0;
    private const int Comparison = 1;
    private const int ReferenceOption = 2;
    private const int Number = 3;
    private const int ReferenceTarget = 5;
    private const int FromVariable = 1;

    public override int Code => ConditionCodes.VariableValue;
    public override bool AllowsWallFurni => true;
    public override byte QuantifierType => WiredQuantifierTypes.Variables;
    public override IReadOnlyList<WiredSlot> FurniSlots =>
        [WiredSlot.EitherPicks.DefaultingTo(WiredSources.TriggerFurni), WiredSlot.EitherPicks.DefaultingTo(WiredSources.SelectedFurni)];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users, WiredSlot.Users.DefaultingTo(WiredSources.SelectedUsers)];
    public override IReadOnlyList<int> DefaultIntParams =>
        [WiredVariableTargets.User, WiredComparison.HigherThan, 0, 0, 0, WiredVariableTargets.Global];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override bool Passes(WiredBox box, WiredRun run)
    {
        if (WiredVariableUse.Variable(box, run.Room) is not { } variable
            || WiredVariableUse.Reference(box, run, box.IntParam(ReferenceOption) == FromVariable,
                WiredVariableUse.Number(box, Number), box.IntParam(ReferenceTarget)) is not { } reference)
            return false;

        return WiredQuantity.Passes(box, IsNegative, WiredVariableUse.Holders(box, run, box.IntParam(Target)), holder =>
            run.Room.VariableValue(variable, holder, run) is { } value
            && WiredComparison.Holds(box.IntParam(Comparison), value, reference), emptyByQuantifier: true);
    }
}

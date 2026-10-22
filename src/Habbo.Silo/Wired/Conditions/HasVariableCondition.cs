using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when its furni, its users, the room or the run hold the chosen variable — all
/// or any of them, as the quantifier says.</summary>
[WiredBox("wf_cnd_has_var")]
public class HasVariableCondition : WiredCondition
{
    public override int Code => ConditionCodes.HasVariable;
    public override bool AllowsWallFurni => true;
    public override byte QuantifierType => WiredQuantifierTypes.Variables;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.Furni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users];
    public override IReadOnlyList<int> DefaultIntParams => [WiredVariableTargets.User];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override bool Passes(WiredBox box, WiredRun run) =>
        WiredVariableUse.Variable(box, run.Room) is { } variable
        && WiredQuantity.Passes(box, IsNegative, WiredVariableUse.Holders(box, run, box.IntParam(0)),
            holder => run.Room.HasVariable(variable, holder, run), emptyByQuantifier: true);
}

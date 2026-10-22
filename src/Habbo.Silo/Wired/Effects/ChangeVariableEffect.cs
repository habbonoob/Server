using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Changes the chosen variable's value for each of its holders by an operation — assign,
/// add, multiply, the bitwise ones — with a number, or another variable's value, as the operand.</summary>
[WiredBox("wf_act_change_var_val")]
public sealed class ChangeVariableEffect : WiredEffect
{
    private const int Target = 0;
    private const int Operation = 1;
    private const int ReferenceOption = 2;
    private const int Number = 3;
    private const int ReferenceTarget = 5;
    private const int FromVariable = 1;

    public override int Code => EffectCodes.ChangeVariable;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots =>
        [WiredSlot.EitherPicks.DefaultingTo(WiredSources.TriggerFurni), WiredSlot.EitherPicks.DefaultingTo(WiredSources.SelectedFurni)];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users, WiredSlot.Users.DefaultingTo(WiredSources.SelectedUsers)];
    public override IReadOnlyList<int> DefaultIntParams => [WiredVariableTargets.User, WiredVariableUse.Assign, 0, 0, 0, WiredVariableTargets.Global];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        if (WiredVariableUse.Variable(box, run.Room) is not { CanWriteValue: true } variable)
            return;

        var operation = box.IntParam(Operation);
        var operand = WiredVariableUse.TakesOperand(operation)
            ? WiredVariableUse.Reference(box, run, box.IntParam(ReferenceOption) == FromVariable,
                WiredVariableUse.Number(box, Number), box.IntParam(ReferenceTarget))
            : 0;
        if (operand is null)
            return;

        foreach (var holder in WiredVariableUse.Holders(box, run, box.IntParam(Target)))
        {
            if (run.Room.VariableValue(variable, holder, run) is { } value
                && WiredVariableUse.Apply(operation, value, operand.Value, run.Room.Random) is { } result)
                await run.Room.SetVariableAsync(variable, holder, run, result);
        }
    }
}

using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Takes the chosen variable from its furni, its users, or the run.</summary>
[WiredBox("wf_act_remove_var")]
public sealed class RemoveVariableEffect : WiredEffect
{
    public override int Code => EffectCodes.RemoveVariable;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.Furni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users];
    public override IReadOnlyList<int> DefaultIntParams => [WiredVariableTargets.User];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        if (WiredVariableUse.Variable(box, run.Room) is not { } variable)
            return;

        foreach (var holder in WiredVariableUse.Holders(box, run, box.IntParam(0)))
            await run.Room.RemoveVariableAsync(variable, holder, run);
    }
}

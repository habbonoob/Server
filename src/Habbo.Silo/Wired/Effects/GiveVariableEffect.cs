using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Gives its furni, its users, the room or the run the chosen variable, with an initial
/// value; those who have it already keep their value unless the dialog says to override it.</summary>
[WiredBox("wf_act_give_var")]
public sealed class GiveVariableEffect : WiredEffect
{
    private const int Target = 0;
    private const int InitialValue = 1;
    private const int Override = 3;

    public override int Code => EffectCodes.GiveVariable;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.Furni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users];
    public override IReadOnlyList<int> DefaultIntParams => [WiredVariableTargets.User, 0, 0, 0];

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        if (WiredVariableUse.Variable(box, run.Room) is not { } variable)
            return;

        var value = WiredVariableUse.Number(box, InitialValue);
        foreach (var holder in WiredVariableUse.Holders(box, run, box.IntParam(Target)))
            await run.Room.GiveVariableAsync(variable, holder, run, value, box.Flag(Override));
    }
}

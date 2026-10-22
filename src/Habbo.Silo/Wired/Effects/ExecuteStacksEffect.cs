namespace Habbo.Silo.Wired.Effects;

/// <summary>Runs the effects of the stacks the picked wired boxes stand in, whatever those stacks'
/// triggers and conditions say.</summary>
[WiredBox("wf_act_call_stacks")]
public class ExecuteStacksEffect : WiredEffect
{
    public override int Code => EffectCodes.CallAnotherStack;
    public override bool PicksFurni => true;

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        // one run per stack however many of its boxes were picked, and never this box's own stack
        var stacks = run.Furni(box)
            .Where(item => (item.X, item.Y) != (box.Item.X, box.Item.Y))
            .DistinctBy(item => (item.X, item.Y))
            .ToList();
        foreach (var item in stacks)
            await run.Room.RunStackEffectsAsync(item, run);
    }
}

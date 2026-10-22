namespace Habbo.Silo.Wired.Effects;

/// <summary>Steps each of its furni on to its next state, or back to its previous one, as the
/// dialog chooses ("Toggle to the next state" / "Toggle to the previous state").</summary>
[WiredBox("wf_act_toggle_state")]
public sealed class ToggleStateEffect : WiredEffect
{
    private const int Direction = 0;
    private const int Previous = 1;

    public override int Code => EffectCodes.ToggleFurniState;
    public override bool AllowsWallFurni => true;
    public override bool PicksFurni => true;
    public override IReadOnlyList<int> DefaultIntParams => [0];

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var step = box.IntParam(Direction) == Previous ? -1 : 1;
        foreach (var item in run.Furni(box).Where(WiredFurni.HasStates).ToList())
        {
            var states = WiredFurni.States(item);
            var at = Math.Max(0, states.IndexOf(WiredFurni.CurrentState(item)));
            var next = states[(at + step + states.Count) % states.Count];
            await run.Room.SetStateAsync(item, WiredFurni.StateText(next));
        }
    }
}

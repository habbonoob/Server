namespace Habbo.Silo.Wired.Effects;

/// <summary>Steps each of its furni on to its next state, or back to its previous one, as the
/// dialog chooses ("Toggle to the next state" / "Toggle to the previous state"). Furnidata says it changes a furni
/// "as if the Furni has just been double clicked", so a floor switch, which answers only an avatar beside it, stays
/// as it is: habbo.com, October 2026, wf_floor_switch1 kept its state while the lamp picked with it switched.</summary>
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
        foreach (var item in run.Furni(box).Where(item => WiredFurni.HasStates(item) && !WiredFurni.IsFloorSwitch(item)).ToList())
        {
            var states = WiredFurni.States(item);
            var at = Math.Max(0, states.IndexOf(WiredFurni.CurrentState(item)));
            var next = states[(at + step + states.Count) % states.Count];
            await run.Room.SetStateAsync(item, WiredFurni.StateText(next));
        }
    }
}

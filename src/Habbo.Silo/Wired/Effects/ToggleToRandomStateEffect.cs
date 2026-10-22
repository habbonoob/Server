namespace Habbo.Silo.Wired.Effects;

/// <summary>Puts each picked furni into a random state other than the one it shows.</summary>
[WiredBox("wf_act_toggle_to_rnd")]
public sealed class ToggleToRandomStateEffect : WiredEffect
{
    public override int Code => EffectCodes.ToggleToRandomState;
    public override bool AllowsWallFurni => true;
    public override bool PicksFurni => true;

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        foreach (var item in run.Furni(box).Where(WiredFurni.HasStates).ToList())
        {
            var others = WiredFurni.States(item).Where(state => state != WiredFurni.CurrentState(item)).ToList();
            await run.Room.SetStateAsync(item, WiredFurni.StateText(others[run.Room.Random.Next(others.Count)]));
        }
    }
}

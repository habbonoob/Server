namespace Habbo.Silo.Wired.Effects;

/// <summary>Starts the room's wired timer again, for "at given time" and the time conditions.</summary>
[WiredBox("wf_act_reset_timers")]
public sealed class ResetTimersEffect : WiredEffect
{
    public override int Code => EffectCodes.Reset;

    public override Task RunAsync(WiredBox box, WiredRun run)
    {
        run.Room.ResetTimers();
        return Task.CompletedTask;
    }
}

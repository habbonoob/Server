namespace Habbo.Silo.Wired.Effects;

/// <summary>Lets each of its users walk again, and takes off their freeze effect.</summary>
[WiredBox("wf_act_unfreeze")]
public sealed class UnfreezeUserEffect : WiredEffect
{
    public override int Code => EffectCodes.UnfreezeUser;
    public override bool TargetsUsers => true;

    public override Task RunAsync(WiredBox box, WiredRun run)
    {
        foreach (var user in run.Users(box).ToList())
            run.Room.Unfreeze(user);
        return Task.CompletedTask;
    }
}

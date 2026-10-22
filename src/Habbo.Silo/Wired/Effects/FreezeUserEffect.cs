namespace Habbo.Silo.Wired.Effects;

/// <summary>Freezes each of its users where they stand, wearing the chosen effect; they cannot walk
/// until unfrozen (by the unfreeze effect, or, if the dialog says so, when teleported).</summary>
[WiredBox("wf_act_freeze")]
public sealed class FreezeUserEffect : WiredEffect
{
    private const int Effect = 0;
    private const int UnfreezeOnTeleport = 1;

    public override int Code => EffectCodes.FreezeUser;
    public override bool TargetsUsers => true;
    public override IReadOnlyList<int> DefaultIntParams => [0, 0];

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        foreach (var user in run.Users(box).ToList())
            await run.Room.FreezeAsync(user, box.IntParam(Effect), box.Flag(UnfreezeOnTeleport));
    }
}

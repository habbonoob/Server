namespace Habbo.Silo.Wired.Effects;

/// <summary>Takes its users out of their teams.</summary>
[WiredBox("wf_act_leave_team")]
public sealed class LeaveTeamEffect : WiredEffect
{
    public override int Code => EffectCodes.LeaveTeam;
    public override bool TargetsUsers => true;

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        foreach (var user in run.Users(box).ToList())
            await run.Room.Games.LeaveAsync(user);
    }
}

using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Puts its users in the chosen team, dressed as the chosen team type (Wired, Banzai, Freeze).</summary>
[WiredBox("wf_act_join_team")]
public sealed class JoinTeamEffect : WiredEffect
{
    private const int Team = 0;
    private const int Type = 1;

    public override int Code => EffectCodes.JoinTeam;
    public override bool TargetsUsers => true;
    public override IReadOnlyList<int> DefaultIntParams => [RoomGames.FirstTeam, (int)RoomGames.TeamType.Wired];

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        foreach (var user in run.Users(box).ToList())
            await run.Room.Games.JoinAsync(user, box.IntParam(Team, RoomGames.FirstTeam), (RoomGames.TeamType)box.IntParam(Type));
    }
}

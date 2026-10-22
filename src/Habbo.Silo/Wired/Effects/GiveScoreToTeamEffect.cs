namespace Habbo.Silo.Wired.Effects;

/// <summary>Gives (or takes) points to the chosen team, at most so many times per game.</summary>
[WiredBox("wf_act_give_score_tm")]
public sealed class GiveScoreToTeamEffect : GiveScoreEffect
{
    private const int Team = 2;

    public override int Code => EffectCodes.GiveScoreToPredefinedTeam;
    public override bool TargetsUsers => false;
    public override IReadOnlyList<int> DefaultIntParams => [5, 0, Rooms.RoomGames.FirstTeam];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    /// <summary>The chosen team, counted as one: its times are the box's, whoever set it off.</summary>
    protected override IEnumerable<(int Counted, int Team)> Scorers(WiredBox box, WiredRun run) =>
        [(0, box.IntParam(Team, Rooms.RoomGames.FirstTeam))];
}

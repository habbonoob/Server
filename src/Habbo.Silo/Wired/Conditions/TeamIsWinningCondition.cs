namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when the chosen team is in the chosen place, 1st to 4th; or, with no team chosen, the teams of its
/// users (the triggering user, by default), all or any of them.</summary>
[WiredBox("wf_cnd_team_has_rank")]
public sealed class TeamIsWinningCondition : WiredCondition
{
    private const int Team = 0;
    private const int Placement = 1;

    public override int Code => ConditionCodes.TeamIsWinning;
    public override IReadOnlyList<int> DefaultIntParams => [WiredGames.AnyOrTriggererTeam, 0];
    public override bool TargetsUsers => true;
    public override byte QuantifierType => WiredQuantifierTypes.Users;

    public override bool Passes(WiredBox box, WiredRun run)
    {
        var ranking = run.Room.Games.Ranking();
        var place = box.IntParam(Placement);
        bool Holds(int team) => team != Rooms.RoomGames.NoTeam && place >= 0 && place < ranking.Count && ranking[place] == team;

        return box.IntParam(Team) == WiredGames.AnyOrTriggererTeam
            ? WiredQuantity.Passes(box, IsNegative, run.Users(box), user => Holds(user.Team))
            : Holds(box.IntParam(Team));
    }
}

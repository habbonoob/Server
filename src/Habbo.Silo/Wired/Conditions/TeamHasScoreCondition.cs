namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when the chosen team's score is lower than, equal to or higher than the box's points; or, with no
/// team chosen, the teams of its users (the triggering user, by default), all or any of them.</summary>
[WiredBox("wf_cnd_team_has_score")]
public sealed class TeamHasScoreCondition : WiredCondition
{
    public override int Code => ConditionCodes.TeamHasScore;
    public override IReadOnlyList<int> DefaultIntParams => [WiredGames.AnyOrTriggererTeam, 0, 0];
    public override bool TargetsUsers => true;
    public override byte QuantifierType => WiredQuantifierTypes.Users;

    public override bool Passes(WiredBox box, WiredRun run)
    {
        bool Holds(int team) => team != Rooms.RoomGames.NoTeam && WiredGames.Compare(run.Room.Games.Score(team), box.IntParam(2), box.IntParam(1));

        return box.IntParam(0) == WiredGames.AnyOrTriggererTeam
            ? WiredQuantity.Passes(box, IsNegative, run.Users(box), user => Holds(user.Team))
            : Holds(box.IntParam(0));
    }
}

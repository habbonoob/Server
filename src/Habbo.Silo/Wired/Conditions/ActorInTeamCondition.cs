namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when its users are in the chosen team (or in any team).</summary>
[WiredBox("wf_cnd_actor_in_team")]
public class ActorInTeamCondition : WiredUserCondition
{
    public override int Code => ConditionCodes.ActorIsInTeam;
    public override IReadOnlyList<int> DefaultIntParams => [WiredGames.AnyOrTriggererTeam];

    public override bool Matches(Rooms.RoomEntityStore.RoomEntity user, WiredBox box, WiredRun run)
    {
        var team = box.IntParam(0);
        return team == WiredGames.AnyOrTriggererTeam ? user.Team != Rooms.RoomGames.NoTeam : user.Team == team;
    }
}

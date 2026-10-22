using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses the users in the chosen team, or in any team.</summary>
[WiredBox("wf_slc_users_team")]
public sealed class UsersInTeamSelector : WiredUserSelector
{
    public override int Code => SelectorCodes.UsersInTeam;
    public override IReadOnlyList<int> DefaultIntParams => [WiredGames.AnyOrTriggererTeam];

    public override IEnumerable<RoomEntityStore.RoomEntity> Select(WiredBox box, WiredRun run)
    {
        var team = box.IntParam(0);
        return run.Room.Entities.Entities.Where(user =>
            team == WiredGames.AnyOrTriggererTeam ? user.Team != RoomGames.NoTeam : user.Team == team);
    }
}

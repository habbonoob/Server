using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses every member of the chosen group in the room, or with none chosen the room's group's.</summary>
[WiredBox("wf_slc_users_group")]
public sealed class UsersInGroupSelector : WiredUserSelector
{
    public override int Code => SelectorCodes.UsersInGroup;

    public override IEnumerable<RoomEntityStore.RoomEntity> Select(WiredBox box, WiredRun run) =>
        WiredGroups.Chosen(box, run.Room) is { } group ? run.Room.Entities.Entities.Where(user => user.GroupIds.Contains(group)) : [];
}

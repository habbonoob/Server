using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses the users standing on the picked furni.</summary>
[WiredBox("wf_slc_users_onfurni")]
public sealed class UsersOnFurniSelector : WiredUserSelector
{
    public override int Code => SelectorCodes.UsersOnFurni;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.SelectorFurni];

    public override IEnumerable<RoomEntityStore.RoomEntity> Select(WiredBox box, WiredRun run)
    {
        var furni = run.Furni(box).ToList();
        return run.Room.Entities.Entities.Where(user => furni.Any(item => WiredFurni.IsUnder(user, item)));
    }
}

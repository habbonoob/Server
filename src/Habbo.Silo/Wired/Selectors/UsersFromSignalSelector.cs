using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses the users the signal that started its stack carried.</summary>
[WiredBox("wf_slc_users_signal")]
public sealed class UsersFromSignalSelector : WiredUserSelector
{
    public override int Code => SelectorCodes.UsersFromSignal;

    public override IEnumerable<RoomEntityStore.RoomEntity> Select(WiredBox box, WiredRun run) =>
        run.Event.SignalUsers.Where(run.Room.IsPresent);
}

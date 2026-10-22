namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a user leaves the room.</summary>
[WiredBox("wf_trg_leave_room")]
public sealed class LeavesRoomTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.AvatarLeavesRoom;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) => e.Type == WiredEventType.UserLeft;
}

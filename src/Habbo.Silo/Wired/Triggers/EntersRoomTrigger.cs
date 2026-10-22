namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a user enters the room. (Which users count is for its stack's conditions.)</summary>
[WiredBox("wf_trg_enter_room")]
public sealed class EntersRoomTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.AvatarEntersRoom;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) => e.Type == WiredEventType.UserEntered;
}

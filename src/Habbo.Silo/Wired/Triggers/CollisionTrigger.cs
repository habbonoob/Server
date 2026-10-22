namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when furni moved by wired runs into a user (who is the triggering user, and the
/// furni the triggering furni).</summary>
[WiredBox("wf_trg_collision")]
public sealed class CollisionTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.AvatarCaught;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) => e.Type == WiredEventType.Collision;
}

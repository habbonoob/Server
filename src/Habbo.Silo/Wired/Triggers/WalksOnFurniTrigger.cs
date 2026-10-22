namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a user steps onto one of the picked furni.</summary>
[WiredBox("wf_trg_walks_on_furni")]
public sealed class WalksOnFurniTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.WalksOnFurni;
    public override bool PicksFurni => true;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.UserWalkedOn, Item: { } item } && Watches(box, item, e, room);
}

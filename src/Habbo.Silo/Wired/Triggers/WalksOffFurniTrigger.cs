namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a user steps off one of the picked furni.</summary>
[WiredBox("wf_trg_walks_off_furni")]
public sealed class WalksOffFurniTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.WalksOffFurni;
    public override bool PicksFurni => true;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.UserWalkedOff, Item: { } item } && Watches(box, item, e, room);
}

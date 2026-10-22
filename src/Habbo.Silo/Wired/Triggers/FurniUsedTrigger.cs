namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a user changes the state of one of the picked furni by using it.</summary>
[WiredBox("wf_trg_state_changed")]
public sealed class FurniUsedTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.UseStuff;
    public override bool AllowsWallFurni => true;
    public override bool PicksFurni => true;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.FurniStateChanged, Item: { } item } && Watches(box, item, e, room);
}

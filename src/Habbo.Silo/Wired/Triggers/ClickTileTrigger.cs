namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a user clicks one of the picked invisible click tiles.</summary>
[WiredBox("wf_trg_click_tile")]
public sealed class ClickTileTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.AvatarClicksTile;
    public override bool PicksFurni => true;
    public override WiredPickRule? PickRule => new(WiredFurni.IsClickTile, "wiredfurni.error.require_click_tiles");

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.FurniClicked, Item: { } item }
        && WiredFurni.IsClickTile(item)
        && Watches(box, item, e, room);
}

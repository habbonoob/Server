namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a user clicks one of the picked furni.</summary>
[WiredBox("wf_trg_click_furni")]
public sealed class ClickFurniTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.AvatarClicksFurni;
    public override bool AllowsWallFurni => true;
    public override bool PicksFurni => true;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.FurniClicked, Item: { } item } && Watches(box, item, e, room);
}

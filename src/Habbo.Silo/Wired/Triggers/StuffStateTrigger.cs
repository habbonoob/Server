namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when one of the picked furni changes state, however it changed: for every state,
/// or only when it comes back to the state it was in when the box was saved.</summary>
[WiredBox("wf_trg_stuff_state")]
public sealed class StuffStateTrigger : WiredTrigger
{
    // state_trigger.0 and .1
    private const int SavedStateOnly = 1;

    public override int Code => TriggerCodes.StateChange;
    public override bool AllowsWallFurni => true;
    public override bool PicksFurni => true;
    public override bool TakesSnapshot => true;
    public override IReadOnlyList<int> DefaultIntParams => [1];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room)
    {
        if (e is not { Type: WiredEventType.FurniStateSet, Item: { } item } || !Watches(box, item, e, room))
            return false;

        return box.IntParam(0) != SavedStateOnly
               || box.Settings.Snapshots.Any(snapshot => snapshot.ItemId == item.Item.Id && snapshot.State == item.Data.State);
    }
}

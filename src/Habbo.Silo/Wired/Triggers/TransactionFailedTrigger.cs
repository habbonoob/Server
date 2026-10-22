namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a user's transaction under one of its contracts fails; why is in @event.transaction_failed.reason.</summary>
[WiredBox("wf_trg_transaction_fail")]
public sealed class TransactionFailedTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.TransactionFailed;
    public override bool PicksFurni => true;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.TransactionFailed, Item: { } contract } && Watches(box, contract, e, room);
}

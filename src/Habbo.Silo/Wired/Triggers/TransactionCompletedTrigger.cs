namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a user's transaction under one of its contracts goes through.</summary>
[WiredBox("wf_trg_transaction_complete")]
public sealed class TransactionCompletedTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.TransactionCompleted;
    public override bool PicksFurni => true;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.TransactionCompleted, Item: { } contract } && Watches(box, contract, e, room);
}

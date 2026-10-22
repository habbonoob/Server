namespace Habbo.Silo.Wired.Effects;

/// <summary>Calls off its users' ongoing payments or trades: those under its contracts, or any.</summary>
[WiredBox("wf_act_cancel_transaction")]
public sealed class CancelTransactionEffect : WiredEffect
{
    private const int Criteria = 0;
    private const int AnyTransaction = 1;

    public override int Code => EffectCodes.CancelTransaction;
    public override bool PicksFurni => true;
    public override bool TargetsUsers => true;
    public override IReadOnlyList<int> DefaultIntParams => [0];

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        IReadOnlySet<int>? contracts = box.IntParam(Criteria) == AnyTransaction ? null : run.Furni(box).Select(c => c.Item.Id).ToHashSet();
        foreach (var user in run.Users(box).ToList())
            await run.Room.Chests.CancelContractsAsync(user, contracts);
    }
}

using Habbo.Silo.Chests;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Starts a transaction for each of its users under its contract, paying into and giving from its chests: the
/// contract's rules met once, exactly so many times, or as often as the user offers up to a limit — the count a number
/// or a variable's value. It may call the transaction off after a while.</summary>
[WiredBox("wf_act_init_transaction")]
public sealed class InitiateTransactionEffect : WiredEffect
{
    private const int Mode = 0;
    private const int Number = 1;
    private const int ReferenceOption = 2;
    private const int ReferenceTarget = 3;
    private const int TimeoutEnabled = 4;
    private const int TimeoutSeconds = 5;
    private const int FromVariable = 1;

    /// <summary>The timeouts the dialog allows, in seconds.</summary>
    private const int MinTimeout = 30;
    private const int MaxTimeout = 3600;

    /// <summary>The most times a contract may be multiplied, as the dialog allows.</summary>
    private const int MaxTimes = 500;

    public override int Code => EffectCodes.InitiateTransaction;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots =>
    [
        WiredSlot.EitherPicks, WiredSlot.EitherPicks.DefaultingTo(WiredSources.PickedFurni2),
        WiredSlot.EitherPicks.DefaultingTo(WiredSources.SelectedFurni),
    ];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users, WiredSlot.Users.DefaultingTo(WiredSources.SelectedUsers)];
    public override IReadOnlyList<int> DefaultIntParams => [ContractTerms.Normal, 10, 0, WiredVariableTargets.Context, 0, 300];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var mode = Math.Clamp(box.IntParam(Mode), ContractTerms.Normal, ContractTerms.AutoMultiplier);
        var times = 1;
        if (mode != ContractTerms.Normal)
        {
            var target = box.IntParam(ReferenceTarget);
            var value = WiredVariableUse.Reference(box, run, box.IntParam(ReferenceOption) == FromVariable, box.IntParam(Number, 1), target,
                variableIndex: 0, slot: target == WiredVariableTargets.User ? 1 : 2);
            if (value is not > 0)
                return;
            times = Math.Min(value.Value, MaxTimes);
        }

        // a custom contract standing with it runs in place of a contract furni; otherwise the first contract among its furni
        Rooms.RoomItemStore.PlacedItem? contractFurni = null;
        Habbo.Domain.Furniture.Chests.WiredContract? contract = null;
        if (run.Stack?.FirstOrDefault(b => b.Type is Addons.CustomContractAddon) is { } custom)
        {
            contract = Addons.CustomContractAddon.Build(custom, run);
            contractFurni = run.Room.FindItem(custom.Id);
        }

        foreach (var candidate in contract is null ? run.Furni(box, 1) : [])
        {
            if (await run.Room.Chests.ContractOfAsync(candidate) is { } found)
            {
                contractFurni = candidate;
                contract = found;
                break;
            }
        }

        if (contract is null || contractFurni is null)
            return;

        var terms = new ContractTerms(contract, mode, times, run.Furni(box, 0).Select(c => c.Item.Id).ToList(), contractFurni.Item.Id);
        var timeout = box.Flag(TimeoutEnabled) ? Math.Clamp(box.IntParam(TimeoutSeconds), MinTimeout, MaxTimeout) : 0;
        // bots and pets have no account to pay or be paid from
        foreach (var user in run.Users(box).Where(user => user.Bot is null && user.Pet is null).ToList())
            await run.Room.Chests.StartContractAsync(user, terms, timeout);
    }
}

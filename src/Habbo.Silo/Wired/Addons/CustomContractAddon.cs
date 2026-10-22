using Habbo.Domain.Furniture.Chests;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Addons;

/// <summary>A contract made of wired, for the initiate transaction effect it stands with: a payment, a reward, or both
/// (a trade), each so many credits or so many items of the kinds its example furni show, the amounts numbers or
/// variables' values. The effect runs it in place of a contract furni.</summary>
[WiredBox("wf_xtra_custom_contract")]
public sealed class CustomContractAddon : WiredAddon
{
    private const int PaymentEnabled = 0;
    private const int PaymentType = 1;
    private const int PaymentOption = 2;
    private const int PaymentNumber = 3;
    private const int PaymentTarget = 4;
    private const int RewardEnabled = 5;
    private const int RewardType = 6;
    private const int RewardOption = 7;
    private const int RewardNumber = 8;
    private const int RewardTarget = 9;
    private const int FromVariable = 1;

    /// <summary>The most one element may ask for or give, as the dialog allows.</summary>
    private const int MaxAmount = 100000;

    public override int Code => AddonCodes.CustomContract;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots =>
    [
        WiredSlot.EitherPicks.DefaultingTo(WiredSources.TriggerFurni), WiredSlot.EitherPicks.DefaultingTo(WiredSources.TriggerFurni),
        WiredSlot.EitherPicks.DefaultingTo(WiredSources.SelectedFurni), WiredSlot.EitherPicks.DefaultingTo(WiredSources.SelectedFurni),
    ];
    public override IReadOnlyList<WiredSlot> UserSlots =>
        [WiredSlot.Users.DefaultingTo(WiredSources.SelectedUsers), WiredSlot.Users.DefaultingTo(WiredSources.SelectedUsers)];
    public override IReadOnlyList<int> DefaultIntParams => [0, ContractNode.Credits, 0, 1, WiredVariableTargets.Context, 0, ContractNode.Credits, 0, 1, WiredVariableTargets.Context];
    public override IReadOnlyList<int> ResetIntParams => [WiredVariableTargets.Context];

    /// <summary>The contract the add-on makes for this run; none when it asks for and gives nothing, or an amount
    /// comes from a variable that has no value.</summary>
    public static WiredContract? Build(WiredBox addon, WiredRun run)
    {
        var payment = addon.Flag(PaymentEnabled)
            ? Element(addon, run, PaymentType, PaymentOption, PaymentNumber, PaymentTarget, element: 0)
            : null;
        var reward = addon.Flag(RewardEnabled)
            ? Element(addon, run, RewardType, RewardOption, RewardNumber, RewardTarget, element: 1)
            : null;
        if ((addon.Flag(PaymentEnabled) && payment is null) || (addon.Flag(RewardEnabled) && reward is null) || (payment is null && reward is null))
            return null;

        var type = payment is not null && reward is not null ? WiredContract.Trade
            : payment is not null ? WiredContract.Payment
            : WiredContract.Reward;
        var contract = WiredContract.Blank(addon.Id, type);
        contract.PaymentMode = WiredContract.SpecificMode;
        contract.YouGive = payment is null ? null : [payment];
        contract.YouGet = reward;
        return contract;
    }

    /// <summary>One side of the contract: credits, or each kind its example furni show, so many of it.</summary>
    private static ContractRule? Element(WiredBox addon, WiredRun run, int typeParam, int optionParam, int numberParam, int targetParam, int element)
    {
        var target = addon.IntParam(targetParam);
        var amount = WiredVariableUse.Reference(addon, run, addon.IntParam(optionParam) == FromVariable, addon.IntParam(numberParam, 1), target,
            variableIndex: element, slot: target == WiredVariableTargets.User ? element : element + 2);
        if (amount is not > 0)
            return null;

        amount = Math.Min(amount.Value, MaxAmount);
        if (addon.IntParam(typeParam) != ContractNode.Furni)
            return new ContractRule { Nodes = [new ContractNode { Type = ContractNode.Credits, Amount = amount.Value }] };

        var kinds = run.Furni(addon, element).Select(run.Room.Chests.KindOf).Distinct().ToList();
        return kinds.Count == 0
            ? null
            : new ContractRule
            {
                Nodes = kinds.Select(k => new ContractNode
                {
                    Type = ContractNode.Furni, Amount = amount.Value, IsWallItem = k.IsWallItem, TypeId = k.ClassId, PosterId = k.PosterId,
                }).ToList(),
            };
    }
}

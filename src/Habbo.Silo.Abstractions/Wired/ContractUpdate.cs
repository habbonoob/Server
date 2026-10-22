using Orleans;

namespace Habbo.Silo.Abstractions.Wired;

/// <summary>What the contract dialog saves: its rules, and what its kind adds (a payment's mode, text and layout; a
/// reward's earnings category and pop-up).</summary>
[GenerateSerializer]
public sealed partial record ContractUpdate(
    [property: Id(0)] int ContractId,
    [property: Id(1)] short ContractType,
    [property: Id(2)] IReadOnlyList<ContractRuleView>? YouGive,
    [property: Id(3)] ContractRuleView? YouGet,
    [property: Id(4)] short? PaymentMode,
    [property: Id(5)] string? ReceiveText,
    [property: Id(6)] string? LayoutType,
    [property: Id(7)] short? RewardCategory,
    [property: Id(8)] bool? ShowDialog,
    [property: Id(9)] string? RewardText);

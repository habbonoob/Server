using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Contracts;

/// <summary>WiredUpdateContractMessageEvent — a contract's requirements saved; a payment contract adds how it is paid
/// and what it says the user gets, a reward contract its earnings category and pop-up.</summary>
[Incoming(ClientMessages.WiredUpdateContractMessageEvent)]
public sealed partial record WiredUpdateContractMessageEvent(
    int ContractId,
    short ContractType,
    TradeRequirementRulesDefinitionData Definition,
    [property: WireWhen("IsPayment")] short? PaymentMode,
    [property: WireWhen("IsPayment")] string? ReceiveText,
    [property: WireWhen("IsPayment")] string? LayoutType,
    [property: WireWhen("IsReward")] short? RewardCategory,
    [property: WireWhen("IsReward")] bool? ShowDialog,
    [property: WireWhen("IsReward")] string? RewardText)
{
    public const short Payment = 0;
    public const short Trade = 1;
    public const short Reward = 2;

    public bool IsPayment => ContractType == Payment;

    public bool IsReward => ContractType == Reward;
}

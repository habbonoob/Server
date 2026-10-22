using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Contracts;

/// <summary>WiredContractContentsMessageComposer — a contract's requirements, and what its kind adds, for its dialog.</summary>
[Message(ServerMessages.WiredContractContentsMessageComposer)]
public sealed partial record WiredContractContentsMessageComposer(
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
    public bool IsPayment => ContractType == Incoming.UserDefinedRoomEvents.WiredTrading.Contracts.WiredUpdateContractMessageEvent.Payment;

    public bool IsReward => ContractType == Incoming.UserDefinedRoomEvents.WiredTrading.Contracts.WiredUpdateContractMessageEvent.Reward;
}

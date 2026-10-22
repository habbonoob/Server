using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Contracts;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;
using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Gateway.Messages;

/// <summary>The contract dialog's save, as the room takes it.</summary>
internal static class ContractUpdates
{
    public static ContractUpdate From(WiredUpdateContractMessageEvent message) => new(
        message.ContractId, message.ContractType, message.Definition.YouGive?.Select(Rule).ToList(),
        message.Definition.YouGet is { } get ? Rule(get) : null, message.PaymentMode, message.ReceiveText, message.LayoutType,
        message.RewardCategory, message.ShowDialog, message.RewardText);

    private static ContractRuleView Rule(TradeRequirementRuleData rule) =>
        new(rule.Nodes.Select(node => new ContractNodeView(node.Type, node.Amount, node.ItemType?.IsWallItem ?? false,
            node.ItemType?.TypeId ?? 0, node.ItemType?.PosterId ?? string.Empty)).ToList());
}

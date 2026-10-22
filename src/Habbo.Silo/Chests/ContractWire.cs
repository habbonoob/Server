using Habbo.Domain.Furniture.Chests;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests.Storage;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;
using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Silo.Chests;

/// <summary>Contract rules as the client writes them, and back.</summary>
public static class ContractWire
{
    public static TradeRequirementRulesDefinitionData Definition(WiredContract contract) =>
        new(contract.YouGive?.Select(Rule).ToList(), contract.YouGet is { } get ? Rule(get) : null);

    public static TradeRequirementRuleData Rule(ContractRule rule) => new(rule.Nodes.Select(Node).ToList());

    private static TradeRequirementNodeData Node(ContractNode node) => node.Type == ContractNode.Furni
        ? new TradeRequirementNodeData(TradeRequirementNodeData.Furni, node.Amount, new ChestItemTypeData(node.IsWallItem, node.TypeId, node.PosterId))
        : new TradeRequirementNodeData(TradeRequirementNodeData.Coins, node.Amount, null);

    /// <summary>A rule the dialog sent.</summary>
    public static ContractRule Rule(ContractRuleView rule) => new() { Nodes = rule.Nodes.Select(Node).ToList() };

    private static ContractNode Node(ContractNodeView node) => new()
    {
        Type = node.Type == ContractNode.Furni ? ContractNode.Furni : ContractNode.Credits,
        Amount = node.Amount,
        IsWallItem = node.Type == ContractNode.Furni && node.IsWallItem,
        TypeId = node.Type == ContractNode.Furni ? node.TypeId : 0,
        PosterId = node.Type == ContractNode.Furni ? node.PosterId : string.Empty,
    };
}

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;

/// <summary>TradeRequirementRuleData — one way of meeting a requirement: all of its nodes</summary>
[WireStruct]
public sealed partial record TradeRequirementRuleData(IReadOnlyList<TradeRequirementNodeData> Nodes);

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;

/// <summary>TradeRequirementRulesDefinitionData — what the user gives, any one of, and what they get</summary>
[WireStruct]
public sealed partial record TradeRequirementRulesDefinitionData([property: WirePresence] IReadOnlyList<TradeRequirementRuleData>? YouGive, [property: WirePresence] TradeRequirementRuleData? YouGet);

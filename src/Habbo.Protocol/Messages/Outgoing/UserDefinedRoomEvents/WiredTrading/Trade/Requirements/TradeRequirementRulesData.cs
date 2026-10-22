using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;

/// <summary>TradeRequirementRulesData — a contract's requirements and how often they may be met</summary>
[WireStruct]
public sealed partial record TradeRequirementRulesData(TradeRequirementRulesDefinitionData Definition, int Type, [property: WireWhen("IsMultiplier")] int? Multiplier, [property: WireWhen("IsAutoMultiplier")] int? AutoMultiplierMax)
{
    /// <summary>The requirements are met once, a set number of times, or as often as the user offers, up to a limit.</summary>
    public const int Normal = 0;
    public const int MultiplierType = 1;
    public const int AutoMultiplier = 2;

    public bool IsMultiplier => Type == MultiplierType;

    public bool IsAutoMultiplier => Type == AutoMultiplier;
}

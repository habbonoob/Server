using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;

/// <summary>TradeRequirementData — what a wired trade asks for</summary>
[WireStruct]
public sealed partial record TradeRequirementData(int Type, string YouGetText, string LayoutType, [property: WireWhen("HasRules")] TradeRequirementRulesData? Rules)
{
    /// <summary>A donation of only credit furni, of anything but credit furni, of anything; or a contract's rules.</summary>
    public const int DonateCredits = 0;
    public const int DonateFurni = 1;
    public const int DonateAnything = 2;
    public const int ContractRules = 4;

    public bool HasRules => Type == ContractRules;
}

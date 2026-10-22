using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>BundleDiscountRulesetData</summary>
[WireStruct]
public sealed partial record BundleDiscountRulesetData(int MaxPurchaseSize, int BundleSize, int BundleDiscountSize, int BonusThreshold, IReadOnlyList<int> AdditionalBonusDiscountThresholdQuantities);

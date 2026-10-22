using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>BundleDiscountRulesetMessageComposer</summary>
[Message(ServerMessages.BundleDiscountRulesetMessageComposer)]
public sealed partial record BundleDiscountRulesetMessageComposer(BundleDiscountRulesetData Ruleset);

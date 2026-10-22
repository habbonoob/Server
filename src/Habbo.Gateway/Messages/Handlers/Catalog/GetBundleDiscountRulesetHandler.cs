using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Catalog;
using Microsoft.Extensions.Options;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetBundleDiscountRuleset — how the client should describe buying many at once.</summary>
internal sealed class GetBundleDiscountRulesetHandler(IOptions<BundleDiscountOptions> options)
    : IMessageHandler<GetBundleDiscountRulesetMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetBundleDiscountRulesetMessageEvent message, CancellationToken ct)
    {
        var rules = options.Value;
        await context.SendAsync(new BundleDiscountRulesetMessageComposer(new BundleDiscountRulesetData(
            rules.MaxPurchaseSize, rules.BundleSize, rules.BundleDiscountSize, rules.BonusThreshold,
            rules.AdditionalBonusDiscountThresholdQuantities)), ct);
        return true;
    }
}

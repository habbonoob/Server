using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketplaceMakeOfferResultComposer — inventory.marketplace.result.N: 1 posted</summary>
[Message(ServerMessages.MarketplaceMakeOfferResultComposer)]
public sealed partial record MarketplaceMakeOfferResultComposer(int Result)
{
    public const int Posted = 1;
    public const int Failed = 2;
}

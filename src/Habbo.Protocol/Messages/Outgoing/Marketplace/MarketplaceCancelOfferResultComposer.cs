using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketplaceCancelOfferResultComposer</summary>
[Message(ServerMessages.MarketplaceCancelOfferResultComposer)]
public sealed partial record MarketplaceCancelOfferResultComposer(int OfferId, bool Success);

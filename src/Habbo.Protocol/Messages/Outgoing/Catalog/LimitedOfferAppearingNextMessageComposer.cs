using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>LimitedOfferAppearingNextMessageComposer — the next limited edition to go on sale</summary>
[Message(ServerMessages.LimitedOfferAppearingNextMessageComposer)]
public sealed partial record LimitedOfferAppearingNextMessageComposer(int AppearsInSeconds, int PageId, int OfferId, string ProductType);

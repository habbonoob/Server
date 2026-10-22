using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketplaceBuyOfferResultComposer — 1 bought; 2 gone; 3 gone, but the same item is on sale at a new price; 4 not enough credits</summary>
[Message(ServerMessages.MarketplaceBuyOfferResultComposer)]
public sealed partial record MarketplaceBuyOfferResultComposer(int Result, int OfferId, int NewPrice, int RequestedOfferId)
{
    public const int Bought = 1;
    public const int Gone = 2;
    public const int PriceChanged = 3;
    public const int NotEnoughCredits = 4;
}

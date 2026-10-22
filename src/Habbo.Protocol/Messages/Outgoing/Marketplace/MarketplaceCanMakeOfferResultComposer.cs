using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketplaceCanMakeOfferResultComposer — 1 may sell; 2 no trading privilege, 3 no trading pass, 4 no tokens, 6 trading lock</summary>
[Message(ServerMessages.MarketplaceCanMakeOfferResultComposer)]
public sealed partial record MarketplaceCanMakeOfferResultComposer(int Result, int TokenCount)
{
    public const int Allowed = 1;
    public const int NoTokens = 4;
    public const int TradingLocked = 6;
}

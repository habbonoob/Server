using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>NftMarketplaceMakeOfferResultMessageComposer (November 2026) — the answer to an NFT offer, which the client
/// shows as it shows <see cref="MarketplaceMakeOfferResultComposer"/>'s, as the text inventory.marketplace.result.N
/// (MarketplaceModel.endOfferMaking, MarketplaceView.showResult).</summary>
[Message(ServerMessages.NftMarketplaceMakeOfferResultMessageComposer)]
public sealed partial record NftMarketplaceMakeOfferResultMessageComposer(int Result)
{
    /// <summary>"Posting the sales offer was successful!"</summary>
    public const int Posted = MarketplaceMakeOfferResultComposer.Posted;

    /// <summary>"Technical error occurred. Posting failed." habbo.com's refusal, sent alone (habbo.com, 2026-11-03).</summary>
    public const int Failed = MarketplaceMakeOfferResultComposer.Failed;

    /// <summary>"Marketplace is disabled." For the NFT marketplace off: unrecorded (the official client never offers while it
    /// is off).</summary>
    public const int Disabled = 3;
}

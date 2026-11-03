using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>NftMarketplaceBuyResultMessageComposer (November 2026) — how buying an NFT went, the offer it was about and
/// its price (MarketPlaceLogic.onNftBuyResult). On <see cref="Success"/> the client asks for its list again; on
/// <see cref="NotEnoughEmeralds"/> it alerts "Not enough Emeralds" / "You do not have enough Emeralds to purchase this
/// Collectible."; on anything else it alerts "Problem with offer" / "Unable to buy the Collectible. Please try again."
/// habbo.com refuses with the offer and price both 0 and sends nothing else (habbo.com, 2026-11-03).</summary>
[Message(ServerMessages.NftMarketplaceBuyResultMessageComposer)]
public sealed partial record NftMarketplaceBuyResultMessageComposer(string Result, long OfferId, int Price)
{
    public const string Success = "SUCCESS";
    public const string NotEnoughEmeralds = "NOT_ENOUGH_EMERALDS";

    /// <summary>No such product on sale: habbo.com's answer for a product code it does not know.</summary>
    public const string Unknown = "UNKNOWN";

    /// <summary>A refusal: the result, offer 0, price 0.</summary>
    public static NftMarketplaceBuyResultMessageComposer Refused(string result) => new(result, 0, 0);
}

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Marketplace;

/// <summary>GetMarketplaceOffersMessageEvent — a search. September 2026 says whether limited editions of a type are
/// listed together (its "combine uniques" box, on by default) or each number on its own. November 2026 says which
/// offers: 0 all, 1 collectibles (NFTs), 2 furni.</summary>
[Incoming(ClientMessages.GetMarketplaceOffersMessageEvent)]
public sealed partial record GetMarketplaceOffersMessageEvent(
    int MinPrice,
    int MaxPrice,
    string SearchQuery,
    int SortOrder,
    [property: WireSince(ClientReleases.September2026)] bool? CombineUniques,
    [property: WireSince(ClientReleases.November2026)] int? OfferSource);

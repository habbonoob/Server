using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Marketplace;

/// <summary>GetMarketplaceOffersMessageEvent — a search. September 2026 says whether limited editions of a type are
/// listed together (its "combine uniques" box, on by default) or each number on its own.</summary>
[Incoming(ClientMessages.GetMarketplaceOffersMessageEvent)]
public sealed partial record GetMarketplaceOffersMessageEvent(
    int MinPrice,
    int MaxPrice,
    string SearchQuery,
    int SortOrder,
    [property: WireSince(ClientReleases.September2026)] bool? CombineUniques);

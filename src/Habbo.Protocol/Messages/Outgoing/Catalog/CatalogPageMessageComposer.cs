using Habbo.Domain.Catalog;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>2082 CatalogPage — one page's offers. The layout code selects the client-side
/// template that renders them, so it has to be a layout the client actually knows.
/// The client reads front page items only if bytes remain, so a page that is not a front page stops here.</summary>
[Message(ServerMessages.CatalogPageMessageComposer)]
public sealed partial record CatalogPageMessageComposer(
    int PageId,
    string CatalogType,
    string LayoutCode,
    CatalogLocalizationData Localization,
    IReadOnlyList<CatalogOfferData> Offers,
    int OfferId,
    bool AcceptSeasonCurrencyAsCredits,
    [property: WireWhen("HasFrontPageItems")] IReadOnlyList<FrontPageItemData>? FrontPageItems = null)
{
    public bool HasFrontPageItems => FrontPageItems is not null;

    /// <summary>The client's "no particular offer" value: show the page, don't select anything.</summary>
    public const int NoOffer = -1;
}

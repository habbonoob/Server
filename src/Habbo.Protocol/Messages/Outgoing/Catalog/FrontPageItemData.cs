using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>One promotion on the catalog's front page. The type decides what follows it: a page name, an offer id
/// or a product code. The expiry is seconds from now, which the client counts down; -1 for none.</summary>
[WireStruct]
public sealed partial record FrontPageItemData(
    int Position,
    string Name,
    string PromoImage,
    int Type,
    [property: WireWhen("OpensPage")] string? PageName,
    [property: WireWhen("OpensOffer")] int? OfferId,
    [property: WireWhen("OpensProduct")] string? ProductCode,
    int ExpiresInSeconds)
{
    public const int PageType = 0;
    public const int OfferType = 1;
    public const int ProductType = 2;

    /// <summary>The client's "never expires".</summary>
    public const int NoExpiry = -1;

    public bool OpensPage => Type == PageType;
    public bool OpensOffer => Type == OfferType;
    public bool OpensProduct => Type == ProductType;
}

using Habbo.Domain.Catalog;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>3793 GetCatalogPage — one page's offers. A page the player may not see is answered
/// with silence: the client keeps the previous page up rather than showing an error. A front page also carries the
/// promotions shown in its slots.</summary>
internal sealed class GetCatalogPageHandler(IShopCatalog catalog)
    : IMessageHandler<GetCatalogPageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, GetCatalogPageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null)
            return false;

        // the client asks in the mode of the catalog it has open: the shop (NORMAL) or the Builders Club warehouse
        var buildersClub = message.CatalogType == CatalogModes.BuildersClub;
        var page = await catalog.GetPageAsync(message.PageId, player.Rank, ct, buildersClub);
        if (page is not { } found)
            return true;

        var frontPageItems = found.Page.LayoutCode.StartsWith(FrontPageLayoutPrefix, StringComparison.Ordinal)
            ? (await catalog.GetFrontPageItemsAsync(ct)).Select(ToWire).ToList()
            : null;

        await context.SendAsync(
            new CatalogPageMessageComposer(
                found.Page.PageId,
                message.CatalogType,
                found.Page.LayoutCode,
                new CatalogLocalizationData(found.Page.Images, found.Page.Texts),
                found.Offers.Select(CatalogOfferFactory.From).ToList(),
                message.OfferId,
                AcceptSeasonCurrencyAsCredits: false,
                frontPageItems),
            ct);

        return true;
    }

    /// <summary>frontpage4, frontpage_featured and the like: the layouts with promotion slots.</summary>
    private const string FrontPageLayoutPrefix = "frontpage";

    private static FrontPageItemData ToWire(ShopFrontPageItem item) => new(
        item.Position,
        item.Name,
        item.PromoImage,
        (int)item.Type,
        item.Type == CatalogFrontPageItemType.Page ? item.PageName : null,
        item.Type == CatalogFrontPageItemType.Offer ? item.OfferId : null,
        item.Type == CatalogFrontPageItemType.Product ? item.ProductCode : null,
        item.ExpiresAt is { } expires
            ? (int)Math.Max(0, (expires - DateTimeOffset.UtcNow).TotalSeconds)
            : FrontPageItemData.NoExpiry);
}

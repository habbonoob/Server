using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>Turns the catalog tree into the client's nested NodeData. Lives apart from the
/// handler because both the index and an empty catalog need it.</summary>
internal static class CatalogNodeFactory
{
    /// <summary>The client reads exactly one root node, so an empty catalog is an empty root
    /// rather than no node at all — otherwise the packet ends early and the client throws.</summary>
    public static CatalogNodeData From(ShopPage? page) =>
        page is null
            ? new CatalogNodeData(
                Visible: true, Icon: 0, PageId: -1, PageName: "root", Localization: "",
                OfferIds: [], Children: [])
            : new CatalogNodeData(
                page.Visible,
                page.Icon,
                page.PageId,
                page.PageName,
                page.Localization,
                page.OfferIds,
                page.Children.Select(From).ToList());
}

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>One node of the catalog index tree — the client's NodeData. A node is recursive:
/// its children are nodes of the same shape, which is how the catalog's left-hand tree is built.
/// Offer ids let the client jump straight to an offer without opening its page first.</summary>
[WireStruct]
public sealed partial record CatalogNodeData(
    bool Visible,
    int Icon,
    int PageId,
    string PageName,
    string Localization,
    IReadOnlyList<int> OfferIds,
    IReadOnlyList<CatalogNodeData> Children);

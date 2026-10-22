using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>3793 GetCatalogPage — opens one page. The offer id is -1 for a plain page click,
/// and set when the client is jumping straight to an offer from a link or search result.</summary>
[Incoming(ClientMessages.GetCatalogPageEvent)]
public sealed partial record GetCatalogPageEvent(int PageId, int OfferId, string CatalogType);

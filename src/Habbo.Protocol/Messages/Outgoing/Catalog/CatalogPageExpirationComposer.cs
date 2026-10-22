using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>CatalogPageExpirationComposer — when a catalog page stops being sold</summary>
[Message(ServerMessages.CatalogPageExpirationComposer)]
public sealed partial record CatalogPageExpirationComposer(int PageId, string PageName, int SecondsToExpiry, string Image);

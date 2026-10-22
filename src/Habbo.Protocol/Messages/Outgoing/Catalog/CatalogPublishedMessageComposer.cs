using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>CatalogPublishedMessageComposer — the catalog changed; the client drops its copy</summary>
[Message(ServerMessages.CatalogPublishedMessageComposer)]
public sealed partial record CatalogPublishedMessageComposer(bool InstantlyRefreshCatalogue, string? NewFurniDataHash = null);

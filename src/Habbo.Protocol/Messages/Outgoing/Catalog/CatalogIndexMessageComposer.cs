using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>262 CatalogIndex — the page tree, sent when the client opens the catalog.
/// The root node is written inline, not as a list: the client reads exactly one.</summary>
[Message(ServerMessages.CatalogIndexMessageComposer)]
public sealed partial record CatalogIndexMessageComposer(
    CatalogNodeData Root,
    bool NewAdditionsAvailable,
    string CatalogType);

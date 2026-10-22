using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>CatalogPageWithEarliestExpiryMessageComposer — the page that stops being sold soonest</summary>
[Message(ServerMessages.CatalogPageWithEarliestExpiryMessageComposer)]
public sealed partial record CatalogPageWithEarliestExpiryMessageComposer(string PageName, int SecondsToExpiry, string Image);

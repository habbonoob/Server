using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>GetCatalogPageWithEarliestExpiryMessageEvent</summary>
[Incoming(ClientMessages.GetCatalogPageWithEarliestExpiryMessageEvent)]
public sealed partial record GetCatalogPageWithEarliestExpiryMessageEvent;

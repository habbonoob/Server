using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>GetCatalogPageExpirationEvent</summary>
[Incoming(ClientMessages.GetCatalogPageExpirationEvent)]
public sealed partial record GetCatalogPageExpirationEvent(string PageName);

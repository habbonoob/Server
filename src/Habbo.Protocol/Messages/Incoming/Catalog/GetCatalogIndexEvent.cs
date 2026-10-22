using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>2933 GetCatalogIndex — opens the catalog. The type selects which tree to send;
/// the client sends "NORMAL" for the shop and "BUILDERS_CLUB" for the builders' catalogue.</summary>
[Incoming(ClientMessages.GetCatalogIndexEvent)]
public sealed partial record GetCatalogIndexEvent(string CatalogType);

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>ClubGiftSelectedComposer</summary>
[Message(ServerMessages.ClubGiftSelectedComposer)]
public sealed partial record ClubGiftSelectedComposer(string ProductCode, IReadOnlyList<CatalogProductData> Products);

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>The page's images and text blocks. The client's layout code indexes into these
/// positionally — a "default_3x3" layout reads texts[0] as the header and texts[1] as the
/// body — so the order matters more than the count.</summary>
[WireStruct]
public sealed partial record CatalogLocalizationData(
    IReadOnlyList<string> Images,
    IReadOnlyList<string> Texts);

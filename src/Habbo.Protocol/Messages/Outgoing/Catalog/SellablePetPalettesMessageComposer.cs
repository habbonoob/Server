using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>SellablePetPalettesMessageComposer — the palettes a pet offer sells</summary>
[Message(ServerMessages.SellablePetPalettesMessageComposer)]
public sealed partial record SellablePetPalettesMessageComposer(string ProductCode, IReadOnlyList<SellablePetPaletteData> SellablePalettes);

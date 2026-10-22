using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>SellablePetPaletteData — a palette a pet offer may sell</summary>
[WireStruct]
public sealed partial record SellablePetPaletteData(int Type, int BreedId, int PaletteId, bool Sellable, bool Rare);

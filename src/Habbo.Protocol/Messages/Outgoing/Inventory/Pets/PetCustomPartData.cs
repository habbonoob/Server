using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Pets;

/// <summary>PetCustomPartData — a part a pet wears over its own</summary>
[WireStruct]
public sealed partial record PetCustomPartData(int LayerId, int PartId, int PaletteId);

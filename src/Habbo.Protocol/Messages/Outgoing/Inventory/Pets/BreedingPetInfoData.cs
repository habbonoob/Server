using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Pets;

/// <summary>BreedingPetInfoData — a parent in a nest</summary>
[WireStruct]
public sealed partial record BreedingPetInfoData(int WebId, string Name, int Level, string Figure, string Owner);

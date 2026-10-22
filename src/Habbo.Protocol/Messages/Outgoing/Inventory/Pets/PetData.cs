using Habbo.Domain.Pets;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Pets;

/// <summary>PetData — a pet in the inventory</summary>
[WireStruct]
public sealed partial record PetData(
    int Id,
    string Name,
    PetFigureWireData Figure,
    int Level,
    [property: WireSince(ClientReleases.September2026)] int RarityLevel)
{
    public static PetData From(Pet pet) => new(pet.Id, pet.Name, PetFigureWireData.From(pet), pet.Level, pet.Rarity);
}

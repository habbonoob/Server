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
    /// <summary>The rarity of a pet that has none: the inventory shows no badge on it (habbo.com, October 2026: a cat, a
    /// spider and a horse show none).</summary>
    public const int NoRarity = -1;

    /// <summary>The kinds whose inventory badge shows their rarity: monster plants, and the Cursed Dragon (habbo.com sends a
    /// new one with 0). What it counts on a grown dragon (Habbo shows 11 on one) is not yet known.</summary>
    private const int MonsterPlantType = 16, CursedDragonType = 37;

    public static PetData From(Pet pet) => new(pet.Id, pet.Name, PetFigureWireData.From(pet), pet.Level,
        pet.TypeId is MonsterPlantType or CursedDragonType ? pet.Rarity : NoRarity);
}

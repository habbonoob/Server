using Habbo.Domain.Furniture;

namespace Habbo.Domain.Pets;

/// <summary>A kind of furniture that holds a pet: the kind of pet inside, and the palettes it may come in
/// (none listed: any the shop sells).</summary>
public sealed class PetPackage
{
    public int DefinitionId { get; set; }
    public ItemDefinition? Definition { get; set; }

    public int TypeId { get; set; }

    public List<int> Palettes { get; set; } = [];
}

using Habbo.Domain.Furniture;

namespace Habbo.Domain.Crafting;

/// <summary>What an altar makes of certain furniture. An open recipe is listed; a secret one is found
/// by putting its exact ingredients in.</summary>
public sealed class CraftingRecipe
{
    public const int CodeMaxLength = 64;

    public int Id { get; set; }

    public required string Code { get; set; }

    /// <summary>The altar furniture it is made at.</summary>
    public int AltarDefinitionId { get; set; }

    public int ProductDefinitionId { get; set; }
    public ItemDefinition? Product { get; set; }

    public bool IsSecret { get; set; }

    public List<CraftingIngredient> Ingredients { get; set; } = [];
}

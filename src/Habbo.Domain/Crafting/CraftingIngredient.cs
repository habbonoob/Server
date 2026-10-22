using Habbo.Domain.Furniture;

namespace Habbo.Domain.Crafting;

/// <summary>How many of a furniture a recipe takes.</summary>
public sealed class CraftingIngredient
{
    public int RecipeId { get; set; }

    public int DefinitionId { get; set; }
    public ItemDefinition? Definition { get; set; }

    public int Count { get; set; }
}

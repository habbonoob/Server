using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Inventory;

/// <summary>Crafting at an altar in the player's room. Every call answers the player who made it.</summary>
public interface ICraftingService
{
    Task SendCraftableProductsAsync(PlayerData player, int altarId);

    Task SendRecipeAsync(PlayerData player, string productCode);

    Task SendSecretRecipesAvailableAsync(PlayerData player, int altarId, IReadOnlyList<int> itemIds);

    Task CraftAsync(PlayerData player, int altarId, string recipeCode);

    Task CraftSecretAsync(PlayerData player, int altarId, IReadOnlyList<int> itemIds);
}

using Habbo.Domain.Crafting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The altars' recipes.</summary>
public static partial class DbSeeder
{
    private sealed record SeedCrafting(List<SeedAltar>? Altars);

    private sealed record SeedAltar(string Altar, List<SeedRecipe> Recipes);

    private sealed record SeedRecipe(string Code, string Product, bool Secret, List<SeedIngredient> Ingredients);

    private sealed record SeedIngredient(string ClassName, int Count);

    /// <summary>seed/crafting.json. Recipes are matched by code, so one the hotel already has is kept.</summary>
    private static async Task SeedCraftingAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        if (await seedFiles.ReadJsonAsync<SeedCrafting>("crafting.json", ct) is not { } seed)
            return;

        var definitions = await db.ItemDefinitions.ToDictionaryAsync(d => d.ClassName, d => d.Id, ct);
        var known = await db.CraftingRecipes.Select(r => r.Code).ToListAsync(ct);
        var added = 0;
        foreach (var altar in seed.Altars ?? [])
        {
            foreach (var recipe in altar.Recipes.Where(r => !known.Contains(r.Code)))
            {
                var names = recipe.Ingredients.Select(i => i.ClassName).Append(recipe.Product).Append(altar.Altar).ToList();
                if (names.FirstOrDefault(n => !definitions.ContainsKey(n)) is { } missing)
                {
                    logger.LogWarning("crafting.json recipe {Recipe} names {Class}, which is not in furnidata.", recipe.Code, missing);
                    continue;
                }

                db.CraftingRecipes.Add(new CraftingRecipe
                {
                    Code = recipe.Code, AltarDefinitionId = definitions[altar.Altar], ProductDefinitionId = definitions[recipe.Product],
                    IsSecret = recipe.Secret,
                    Ingredients = recipe.Ingredients.Select(i => new CraftingIngredient { DefinitionId = definitions[i.ClassName], Count = i.Count }).ToList(),
                });
                added++;
            }
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} crafting recipe(s).", added);
        }
    }
}

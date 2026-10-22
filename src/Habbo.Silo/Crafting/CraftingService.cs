using Habbo.Domain.Crafting;
using Habbo.Domain.Furniture;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Crafting;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Silo.Abstractions.Inventory;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Orleans;

namespace Habbo.Silo.Crafting;

/// <summary>Crafting. The altar must stand in the room the player is in; the ingredients come out of the
/// player's inventory and the product goes into it, together.</summary>
public sealed class CraftingService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    FurniInteractionRegistry interactions,
    PlayerMessenger messenger) : ICraftingService
{
    /// <summary>The altar's furniture, if it stands in the player's room.</summary>
    private async Task<int?> AltarDefinitionAsync(AppDbContext db, PlayerData player, int altarId)
    {
        var roomId = await grains.GetGrain<IPlayerGrain>(player.Id).GetCurrentRoomAsync();
        return roomId is null ? null : await db.UserItems.AsNoTracking()
            .Where(i => i.Id == altarId && i.RoomId == roomId).Select(i => (int?)i.DefinitionId).FirstOrDefaultAsync();
    }

    private static IQueryable<CraftingRecipe> RecipesAt(AppDbContext db, int altar) =>
        db.CraftingRecipes.AsNoTracking().Include(r => r.Product).Include(r => r.Ingredients).ThenInclude(i => i.Definition)
            .Where(r => r.AltarDefinitionId == altar);

    /// <summary>The player's inventory items that may go into crafting.</summary>
    private static IQueryable<UserItem> Usable(AppDbContext db, Guid playerId) =>
        db.UserItems.Where(i => i.OwnerId == playerId && i.RoomId == null && !i.InMarketplace && i.ChestId == null && !db.UserItems.Any(stored => stored.ChestId == i.Id) && !db.ChestCoins.Any(c => c.ItemId == i.Id && c.Coins > 0));

    public async Task SendCraftableProductsAsync(PlayerData player, int altarId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await AltarDefinitionAsync(db, player, altarId) is not { } altar)
        {
            await messenger.SendAsync(player.Id, new CraftableProductsMessageComposer([], []));
            return;
        }

        var recipes = await RecipesAt(db, altar).ToListAsync();
        var ingredientIds = recipes.SelectMany(r => r.Ingredients).Select(i => i.DefinitionId).Distinct().ToList();
        var held = await Usable(db, player.Id).Where(i => ingredientIds.Contains(i.DefinitionId))
            .Select(i => i.Definition!.ClassName).Distinct().ToListAsync();
        await messenger.SendAsync(player.Id, new CraftableProductsMessageComposer(
            recipes.Where(r => !r.IsSecret).Select(r => new CraftingProductData(r.Code, r.Product!.ClassName, r.Product.ClassName)).ToList(),
            held));
    }

    public async Task SendRecipeAsync(PlayerData player, string productCode)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var recipe = await db.CraftingRecipes.AsNoTracking().Include(r => r.Ingredients).ThenInclude(i => i.Definition)
            .FirstOrDefaultAsync(r => r.Code == productCode && !r.IsSecret);
        await messenger.SendAsync(player.Id, new CraftingRecipeMessageComposer(
            (recipe?.Ingredients ?? []).Select(i => new CraftingIngredientData(i.Count, i.Definition!.ClassName)).ToList()));
    }

    public async Task SendSecretRecipesAvailableAsync(PlayerData player, int altarId, IReadOnlyList<int> itemIds)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var (matching, complete) = await MatchSecretAsync(db, player, altarId, itemIds);
        await messenger.SendAsync(player.Id, new CraftingRecipesAvailableMessageComposer(matching.Count, complete is not null));
    }

    /// <summary>The secret recipes these items could be part of, and the one they make exactly, if any.</summary>
    private async Task<(List<CraftingRecipe> Matching, CraftingRecipe? Complete)> MatchSecretAsync(
        AppDbContext db, PlayerData player, int altarId, IReadOnlyList<int> itemIds)
    {
        if (await AltarDefinitionAsync(db, player, altarId) is not { } altar)
            return ([], null);

        var ids = itemIds.Distinct().ToList();
        var items = await Usable(db, player.Id).AsNoTracking().Where(i => ids.Contains(i.Id)).ToListAsync();
        if (items.Count != ids.Count)
            return ([], null);

        var given = items.GroupBy(i => i.DefinitionId).ToDictionary(g => g.Key, g => g.Count());
        var secrets = await RecipesAt(db, altar).Where(r => r.IsSecret).ToListAsync();
        var matching = secrets.Where(r => given.All(g => r.Ingredients.Any(i => i.DefinitionId == g.Key && i.Count >= g.Value))).ToList();
        var complete = matching.FirstOrDefault(r => r.Ingredients.Count == given.Count
                                                    && r.Ingredients.All(i => given.GetValueOrDefault(i.DefinitionId) == i.Count));
        return (matching, complete);
    }

    public async Task CraftAsync(PlayerData player, int altarId, string recipeCode)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var recipe = await AltarDefinitionAsync(db, player, altarId) is { } altar
            ? await RecipesAt(db, altar).FirstOrDefaultAsync(r => r.Code == recipeCode && !r.IsSecret)
            : null;
        if (recipe is null)
        {
            await messenger.SendAsync(player.Id, new CraftingResultMessageComposer(false, null));
            return;
        }

        // the ingredients, from what the player holds
        var used = new List<UserItem>();
        foreach (var ingredient in recipe.Ingredients)
        {
            var have = await Usable(db, player.Id).Where(i => i.DefinitionId == ingredient.DefinitionId)
                .OrderBy(i => i.Id).Take(ingredient.Count).ToListAsync();
            if (have.Count < ingredient.Count)
            {
                await messenger.SendAsync(player.Id, new CraftingResultMessageComposer(false, null));
                return;
            }

            used.AddRange(have);
        }

        await MakeAsync(db, player, recipe, used.Select(i => i.Id).ToList());
    }

    public async Task CraftSecretAsync(PlayerData player, int altarId, IReadOnlyList<int> itemIds)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var (_, recipe) = await MatchSecretAsync(db, player, altarId, itemIds);
        if (recipe is null)
        {
            await messenger.SendAsync(player.Id, new CraftingResultMessageComposer(false, null));
            return;
        }

        // the very items the recipe was matched against: whatever of them is gone by now fails the craft
        await MakeAsync(db, player, recipe, itemIds.Distinct().ToList());
    }

    /// <summary>Takes the ingredients and hands over the product, in one go.</summary>
    private async Task MakeAsync(AppDbContext db, PlayerData player, CraftingRecipe recipe, List<int> ids)
    {
        // the ingredients go only if they are all still the player's to use: a trade or an offer may have taken one since.
        // What is deleted has to be the whole recipe, not just what was left of the items.
        await using var transaction = await db.Database.BeginTransactionAsync();
        var needed = recipe.Ingredients.Sum(i => i.Count);
        if (ids.Count != needed
            || await Usable(db, player.Id).Where(i => ids.Contains(i.Id)).ExecuteDeleteAsync() != needed)
        {
            await transaction.RollbackAsync();
            await messenger.SendAsync(player.Id, new CraftingResultMessageComposer(false, null));
            return;
        }

        var product = await db.ItemDefinitions.FirstAsync(d => d.Id == recipe.ProductDefinitionId);
        var item = new UserItem { OwnerId = player.Id, DefinitionId = product.Id };
        db.UserItems.Add(item);
        interactions.For(product).Grant(new FurniGrant(db, item, product, string.Empty, string.Empty, player.Username, DateTime.UtcNow));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        await messenger.SendAsync(player.Id, new FurniListInvalidateComposer());
        await messenger.SendAsync(player.Id, new CraftingResultMessageComposer(true,
            new CraftingProductData(recipe.Code, product.ClassName, product.ClassName)));
        await grains.GetGrain<IPlayerGrain>(player.Id).ProgressAchievementAsync(
            recipe.IsSecret ? AchievementNames.SecretCrafting : AchievementNames.Crafting, 1);
    }
}

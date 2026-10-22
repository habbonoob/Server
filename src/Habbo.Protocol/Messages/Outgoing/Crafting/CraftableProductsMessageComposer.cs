using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Crafting;

/// <summary>CraftableProductsMessageComposer — an altar's open recipes, and the ingredients the player holds</summary>
[Message(ServerMessages.CraftableProductsMessageComposer)]
public sealed partial record CraftableProductsMessageComposer(IReadOnlyList<CraftingProductData> RecipeProducts, IReadOnlyList<string> UsableInventoryFurniClasses);

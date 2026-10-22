using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Crafting;

/// <summary>CraftingProductData</summary>
[WireStruct]
public sealed partial record CraftingProductData(string RecipeCode, string ProductCode, string FurnitureClassName);

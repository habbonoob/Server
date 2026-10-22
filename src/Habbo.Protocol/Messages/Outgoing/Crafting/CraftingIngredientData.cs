using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Crafting;

/// <summary>CraftingIngredientData</summary>
[WireStruct]
public sealed partial record CraftingIngredientData(int Count, string FurnitureClassName);

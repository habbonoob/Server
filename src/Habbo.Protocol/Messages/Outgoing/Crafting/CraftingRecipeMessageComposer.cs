using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Crafting;

/// <summary>CraftingRecipeMessageComposer</summary>
[Message(ServerMessages.CraftingRecipeMessageComposer)]
public sealed partial record CraftingRecipeMessageComposer(IReadOnlyList<CraftingIngredientData> Ingredients);

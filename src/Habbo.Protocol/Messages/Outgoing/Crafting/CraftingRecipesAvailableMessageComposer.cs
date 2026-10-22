using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Crafting;

/// <summary>CraftingRecipesAvailableMessageComposer</summary>
[Message(ServerMessages.CraftingRecipesAvailableMessageComposer)]
public sealed partial record CraftingRecipesAvailableMessageComposer(int Count, bool RecipeComplete);

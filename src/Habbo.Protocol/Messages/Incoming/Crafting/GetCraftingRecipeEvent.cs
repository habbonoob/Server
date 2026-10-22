using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Crafting;

/// <summary>GetCraftingRecipeEvent</summary>
[Incoming(ClientMessages.GetCraftingRecipeEvent)]
public sealed partial record GetCraftingRecipeEvent(string ProductCode);

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Crafting;

/// <summary>GetCraftingRecipesAvailableEvent — how many secret recipes these items could be part of</summary>
[Incoming(ClientMessages.GetCraftingRecipesAvailableEvent)]
public sealed partial record GetCraftingRecipesAvailableEvent(int AltarId, IReadOnlyList<int> ItemIds);

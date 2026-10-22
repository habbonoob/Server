using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Crafting;

/// <summary>CraftEvent</summary>
[Incoming(ClientMessages.CraftEvent)]
public sealed partial record CraftEvent(int AltarId, string RecipeCode);

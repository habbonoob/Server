using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Crafting;

/// <summary>GetCraftableProductsEvent — what can be crafted at an altar</summary>
[Incoming(ClientMessages.GetCraftableProductsEvent)]
public sealed partial record GetCraftableProductsEvent(int AltarId);

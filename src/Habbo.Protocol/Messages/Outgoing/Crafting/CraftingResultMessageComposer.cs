using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Crafting;

/// <summary>CraftingResultMessageComposer</summary>
[Message(ServerMessages.CraftingResultMessageComposer)]
public sealed partial record CraftingResultMessageComposer(bool Success, [property: WireWhen("Success")] CraftingProductData? Product);

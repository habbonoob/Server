using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Bots;

/// <summary>BotRemovedFromInventoryComposer — a bot left the inventory</summary>
[Message(ServerMessages.BotRemovedFromInventoryComposer)]
public sealed partial record BotRemovedFromInventoryComposer(int BotId);

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Bots;

/// <summary>BotAddedToInventoryComposer — a bot joined the inventory</summary>
[Message(ServerMessages.BotAddedToInventoryComposer)]
public sealed partial record BotAddedToInventoryComposer(BotData Bot, bool OpenInventory);

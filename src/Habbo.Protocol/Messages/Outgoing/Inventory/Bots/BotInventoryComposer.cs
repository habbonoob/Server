using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Bots;

/// <summary>BotInventoryComposer — the bots the player owns and has not placed</summary>
[Message(ServerMessages.BotInventoryComposer)]
public sealed partial record BotInventoryComposer(IReadOnlyList<BotData> Bots);

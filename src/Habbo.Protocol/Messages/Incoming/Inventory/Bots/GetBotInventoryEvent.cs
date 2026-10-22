using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Bots;

/// <summary>GetBotInventoryEvent — the bots the player owns and has not placed</summary>
[Incoming(ClientMessages.GetBotInventoryEvent)]
public sealed partial record GetBotInventoryEvent;

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Furni;

/// <summary>3765 RequestFurniInventory — asked from inside a room.</summary>
[Incoming(ClientMessages.RequestFurniInventoryEvent)]
public sealed partial record RequestFurniInventoryEvent;

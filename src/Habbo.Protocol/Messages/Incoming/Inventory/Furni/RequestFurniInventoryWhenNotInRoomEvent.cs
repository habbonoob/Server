using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Furni;

/// <summary>2918 RequestFurniInventory — asked from the hotel view.</summary>
[Incoming(ClientMessages.RequestFurniInventoryWhenNotInRoomEvent)]
public sealed partial record RequestFurniInventoryWhenNotInRoomEvent;

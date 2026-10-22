using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Pets;

/// <summary>PetRemovedFromInventoryComposer — a pet left the inventory</summary>
[Message(ServerMessages.PetRemovedFromInventoryComposer)]
public sealed partial record PetRemovedFromInventoryComposer(int PetId);

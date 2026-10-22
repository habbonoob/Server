using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Pets;

/// <summary>PetAddedToInventoryComposer — a pet joined the inventory</summary>
[Message(ServerMessages.PetAddedToInventoryComposer)]
public sealed partial record PetAddedToInventoryComposer(PetData Pet, bool OpenInventory);

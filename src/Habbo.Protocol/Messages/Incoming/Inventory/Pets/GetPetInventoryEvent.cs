using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Pets;

/// <summary>GetPetInventoryEvent — the pets the player owns and has not placed</summary>
[Incoming(ClientMessages.GetPetInventoryEvent)]
public sealed partial record GetPetInventoryEvent;

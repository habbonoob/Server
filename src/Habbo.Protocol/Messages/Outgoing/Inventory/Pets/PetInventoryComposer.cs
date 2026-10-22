using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Pets;

/// <summary>PetInventoryComposer — the pets the player owns and has not placed</summary>
[Message(ServerMessages.PetInventoryComposer)]
public sealed partial record PetInventoryComposer(int TotalFragments, int FragmentNo, IReadOnlyList<PetData> Pets);

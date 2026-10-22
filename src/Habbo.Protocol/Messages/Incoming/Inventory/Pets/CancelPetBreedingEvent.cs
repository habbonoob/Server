using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Pets;

/// <summary>CancelPetBreedingEvent — send the pets in a nest away without breeding them</summary>
[Incoming(ClientMessages.CancelPetBreedingEvent)]
public sealed partial record CancelPetBreedingEvent(int NestId);

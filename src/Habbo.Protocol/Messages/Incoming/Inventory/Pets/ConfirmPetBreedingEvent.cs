using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Pets;

/// <summary>ConfirmPetBreedingEvent — breed the two pets in a nest, naming the baby</summary>
[Incoming(ClientMessages.ConfirmPetBreedingEvent)]
public sealed partial record ConfirmPetBreedingEvent(int NestId, string Name, int PetOneId, int PetTwoId);

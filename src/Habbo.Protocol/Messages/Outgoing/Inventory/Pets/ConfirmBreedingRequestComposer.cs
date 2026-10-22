using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Pets;

/// <summary>ConfirmBreedingRequestComposer — two pets are in the nest: breed them?</summary>
[Message(ServerMessages.ConfirmBreedingRequestComposer)]
public sealed partial record ConfirmBreedingRequestComposer(int NestId, BreedingPetInfoData PetOne, BreedingPetInfoData PetTwo, IReadOnlyList<RarityCategoryData> RarityCategories, int ResultPetType);

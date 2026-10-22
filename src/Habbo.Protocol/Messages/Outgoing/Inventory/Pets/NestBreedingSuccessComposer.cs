using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Pets;

/// <summary>NestBreedingSuccessComposer — the baby that was bred, and how rare it is</summary>
[Message(ServerMessages.NestBreedingSuccessComposer)]
public sealed partial record NestBreedingSuccessComposer(int PetId, int RarityCategory);

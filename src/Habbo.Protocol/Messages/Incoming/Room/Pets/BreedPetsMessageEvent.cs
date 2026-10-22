using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Pets;

/// <summary>BreedPetsMessageEvent — ask to breed two plants, or answer such a request</summary>
[Incoming(ClientMessages.BreedPetsMessageEvent)]
public sealed partial record BreedPetsMessageEvent(int State, int PetOneId, int PetTwoId);

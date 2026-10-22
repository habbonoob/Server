using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Pets;

/// <summary>GetPetInfoMessageEvent — a pet's details</summary>
[Incoming(ClientMessages.GetPetInfoMessageEvent)]
public sealed partial record GetPetInfoMessageEvent(int PetId);

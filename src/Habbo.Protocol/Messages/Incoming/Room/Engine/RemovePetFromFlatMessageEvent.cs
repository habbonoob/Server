using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>RemovePetFromFlatMessageEvent — pick a pet up: it goes back to its owner</summary>
[Incoming(ClientMessages.RemovePetFromFlatMessageEvent)]
public sealed partial record RemovePetFromFlatMessageEvent(int PetId);

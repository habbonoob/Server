using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>RemoveSaddleFromPetMessageEvent — take a pet's saddle off</summary>
[Incoming(ClientMessages.RemoveSaddleFromPetMessageEvent)]
public sealed partial record RemoveSaddleFromPetMessageEvent(int PetId);

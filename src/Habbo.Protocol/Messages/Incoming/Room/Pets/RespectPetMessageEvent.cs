using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Pets;

/// <summary>RespectPetMessageEvent — respect a pet</summary>
[Incoming(ClientMessages.RespectPetMessageEvent)]
public sealed partial record RespectPetMessageEvent(int PetId);

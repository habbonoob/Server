using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>GetPetCommandsMessageEvent — the commands a pet knows and obeys</summary>
[Incoming(ClientMessages.GetPetCommandsMessageEvent)]
public sealed partial record GetPetCommandsMessageEvent(int PetId);

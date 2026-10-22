using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>MountPetMessageEvent — get on or off a pet</summary>
[Incoming(ClientMessages.MountPetMessageEvent)]
public sealed partial record MountPetMessageEvent(int PetId, bool Mount);

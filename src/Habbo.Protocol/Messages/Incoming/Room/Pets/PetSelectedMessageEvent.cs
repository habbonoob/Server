using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Pets;

/// <summary>PetSelectedMessageEvent — the player picked a pet out</summary>
[Incoming(ClientMessages.PetSelectedMessageEvent)]
public sealed partial record PetSelectedMessageEvent(int PetId);

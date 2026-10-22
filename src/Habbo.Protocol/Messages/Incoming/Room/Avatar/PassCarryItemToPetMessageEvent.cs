using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Avatar;

/// <summary>1641 PassCarryItemToPet.</summary>
[Incoming(ClientMessages.PassCarryItemToPetMessageEvent)]
public sealed partial record PassCarryItemToPetMessageEvent(int PetId);

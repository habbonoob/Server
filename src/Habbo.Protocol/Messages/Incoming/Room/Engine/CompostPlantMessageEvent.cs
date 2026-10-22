using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>CompostPlantMessageEvent — turn a dead plant into compost</summary>
[Incoming(ClientMessages.CompostPlantMessageEvent)]
public sealed partial record CompostPlantMessageEvent(int PetId);

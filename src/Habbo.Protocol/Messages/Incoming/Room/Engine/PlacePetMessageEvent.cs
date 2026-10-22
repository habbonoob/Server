using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>PlacePetMessageEvent — put a pet from the inventory down in the room</summary>
[Incoming(ClientMessages.PlacePetMessageEvent)]
public sealed partial record PlacePetMessageEvent(int PetId, int X, int Y);

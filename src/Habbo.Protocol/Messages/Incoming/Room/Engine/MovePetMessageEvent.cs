using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>MovePetMessageEvent — move a pet to another tile</summary>
[Incoming(ClientMessages.MovePetMessageEvent)]
public sealed partial record MovePetMessageEvent(int PetId, int X, int Y, int Direction);

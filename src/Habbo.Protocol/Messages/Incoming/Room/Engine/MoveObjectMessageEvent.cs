using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>3018 MoveObject — move or rotate a floor item.</summary>
[Incoming(ClientMessages.MoveObjectMessageEvent)]
public sealed partial record MoveObjectMessageEvent(int ItemId, int X, int Y, int Rotation);

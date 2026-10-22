using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Ingame;

/// <summary>Game2SetUserMoveTargetMessageEvent — walk towards a point, in game units</summary>
[Incoming(ClientMessages.Game2SetUserMoveTargetMessageEvent)]
public sealed partial record Game2SetUserMoveTargetMessageEvent(int X, int Y, int Turn, int Subturn);

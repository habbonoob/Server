using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Ingame;

/// <summary>Game2ThrowSnowballAtPositionMessageEvent — throw a snowball at a point</summary>
[Incoming(ClientMessages.Game2ThrowSnowballAtPositionMessageEvent)]
public sealed partial record Game2ThrowSnowballAtPositionMessageEvent(int X, int Y, int Trajectory, int Turn, int Subturn);

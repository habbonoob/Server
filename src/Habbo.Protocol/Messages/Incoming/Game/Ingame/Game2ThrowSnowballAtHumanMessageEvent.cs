using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Ingame;

/// <summary>Game2ThrowSnowballAtHumanMessageEvent — throw a snowball at a player</summary>
[Incoming(ClientMessages.Game2ThrowSnowballAtHumanMessageEvent)]
public sealed partial record Game2ThrowSnowballAtHumanMessageEvent(int TargetHumanId, int Trajectory, int Turn, int Subturn);

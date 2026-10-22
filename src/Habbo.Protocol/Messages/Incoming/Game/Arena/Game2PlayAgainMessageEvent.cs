using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Arena;

/// <summary>Game2PlayAgainMessageEvent — play another game with the same players</summary>
[Incoming(ClientMessages.Game2PlayAgainMessageEvent)]
public sealed partial record Game2PlayAgainMessageEvent;

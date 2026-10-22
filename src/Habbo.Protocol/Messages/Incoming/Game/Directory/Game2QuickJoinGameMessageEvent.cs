using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Directory;

/// <summary>Game2QuickJoinGameMessageEvent — join any game waiting for players, or start one</summary>
[Incoming(ClientMessages.Game2QuickJoinGameMessageEvent)]
public sealed partial record Game2QuickJoinGameMessageEvent;

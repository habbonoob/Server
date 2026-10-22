using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Arena;

/// <summary>Game2ExitGameMessageEvent — leave the arena</summary>
[Incoming(ClientMessages.Game2ExitGameMessageEvent)]
public sealed partial record Game2ExitGameMessageEvent(bool ToLobby);

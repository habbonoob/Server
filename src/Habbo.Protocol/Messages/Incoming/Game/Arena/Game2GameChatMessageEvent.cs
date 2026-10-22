using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Arena;

/// <summary>Game2GameChatMessageEvent — say something in the arena</summary>
[Incoming(ClientMessages.Game2GameChatMessageEvent)]
public sealed partial record Game2GameChatMessageEvent(string Message);

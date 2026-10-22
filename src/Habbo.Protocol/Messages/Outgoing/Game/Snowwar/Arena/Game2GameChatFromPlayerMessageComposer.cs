using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

/// <summary>Game2GameChatFromPlayerMessageComposer — a player said something in the arena</summary>
[Message(ServerMessages.Game2GameChatFromPlayerMessageComposer)]
public sealed partial record Game2GameChatFromPlayerMessageComposer(int UserId, string ChatMessage);

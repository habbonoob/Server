using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

/// <summary>Game2PlayerExitedGameArenaMessageComposer — a player left the arena</summary>
[Message(ServerMessages.Game2PlayerExitedGameArenaMessageComposer)]
public sealed partial record Game2PlayerExitedGameArenaMessageComposer(int UserId, int PlayerGameObjectId);

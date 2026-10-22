using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Directory;

/// <summary>Game2InArenaQueueMessageComposer — where the player stands in the queue for a game</summary>
[Message(ServerMessages.Game2InArenaQueueMessageComposer)]
public sealed partial record Game2InArenaQueueMessageComposer(int Position);

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

/// <summary>Game2ArenaEnteredMessageComposer — a player came into the arena</summary>
[Message(ServerMessages.Game2ArenaEnteredMessageComposer)]
public sealed partial record Game2ArenaEnteredMessageComposer(Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data.Game2PlayerData Player);

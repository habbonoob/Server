using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

/// <summary>Game2EnterArenaMessageComposer — the arena, and who plays in it</summary>
[Message(ServerMessages.Game2EnterArenaMessageComposer)]
public sealed partial record Game2EnterArenaMessageComposer(int GameType, int FieldType, int NumberOfTeams, IReadOnlyList<Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data.Game2PlayerData> Players, Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data.GameLevelData Level);

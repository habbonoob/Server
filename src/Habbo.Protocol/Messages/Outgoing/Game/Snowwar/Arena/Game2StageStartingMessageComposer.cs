using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

/// <summary>Game2StageStartingMessageComposer — the round starts after a countdown, with these objects</summary>
[Message(ServerMessages.Game2StageStartingMessageComposer)]
public sealed partial record Game2StageStartingMessageComposer(int GameType, string RoomType, int CountDown, IReadOnlyList<Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data.SnowWarGameObjectData> GameObjects);

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

/// <summary>Game2GameEndingMessageComposer — the game is over: the results</summary>
[Message(ServerMessages.Game2GameEndingMessageComposer)]
public sealed partial record Game2GameEndingMessageComposer(int TimeToNextState, Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data.Game2GameResult Result, IReadOnlyList<Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data.Game2TeamScoreData> Teams, Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data.Game2SnowWarGameStats GeneralStats);

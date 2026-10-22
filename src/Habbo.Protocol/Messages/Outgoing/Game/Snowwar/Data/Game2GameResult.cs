using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

/// <summary>Game2GameResult — how the game ended</summary>
[WireStruct]
public sealed partial record Game2GameResult(bool IsDeathMatch, int ResultType, int WinnerId);

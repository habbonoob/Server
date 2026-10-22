using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>One row of a scoreboard: a score and the players who share it.</summary>
[WireStruct]
public sealed partial record FurniHighScoreEntryData(int Score, IReadOnlyList<string> Users);

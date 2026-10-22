using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

/// <summary>Game2PlayerData — a player in the arena</summary>
[WireStruct]
public sealed partial record Game2PlayerData(int ReferenceId, string UserName, string FigureString, string Gender, int TeamId);

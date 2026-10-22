using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

/// <summary>GameLobbyPlayerData — a player waiting for a game</summary>
[WireStruct]
public sealed partial record GameLobbyPlayerData(int UserId, string Name, string Figure, string Gender, int TeamId, int SkillLevel, int TotalScore, int ScoreToNextLevel);

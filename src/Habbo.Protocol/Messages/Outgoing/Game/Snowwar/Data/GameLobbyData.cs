using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

/// <summary>GameLobbyData — a game waiting for players</summary>
[WireStruct]
public sealed partial record GameLobbyData(int GameId, string LevelName, int GameType, int FieldType, int NumberOfTeams, int MaximumPlayers, string OwningPlayerName, int LevelEntryId, IReadOnlyList<GameLobbyPlayerData> Players);

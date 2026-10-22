using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Directory;

/// <summary>Game2UserJoinedGameMessageComposer — somebody joined the game</summary>
[Message(ServerMessages.Game2UserJoinedGameMessageComposer)]
public sealed partial record Game2UserJoinedGameMessageComposer(Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data.GameLobbyPlayerData User, bool WasTeamSwitched);

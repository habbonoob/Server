using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Directory;

/// <summary>Game2GameCreatedMessageComposer — the player's own new game, waiting for players</summary>
[Message(ServerMessages.Game2GameCreatedMessageComposer)]
public sealed partial record Game2GameCreatedMessageComposer(Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data.GameLobbyData Lobby);

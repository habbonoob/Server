using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Directory;

/// <summary>Game2GameLongDataMessageComposer — the game the player joined, waiting for players</summary>
[Message(ServerMessages.Game2GameLongDataMessageComposer)]
public sealed partial record Game2GameLongDataMessageComposer(Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data.GameLobbyData Lobby);

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Directory;

/// <summary>Game2GameStartedMessageComposer — the game waited for starts: the arena loads</summary>
[Message(ServerMessages.Game2GameStartedMessageComposer)]
public sealed partial record Game2GameStartedMessageComposer(Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data.GameLobbyData Lobby);

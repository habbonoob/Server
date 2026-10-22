using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Directory;

/// <summary>Game2GameCancelledMessageComposer — the game waited for was called off</summary>
[Message(ServerMessages.Game2GameCancelledMessageComposer)]
public sealed partial record Game2GameCancelledMessageComposer;

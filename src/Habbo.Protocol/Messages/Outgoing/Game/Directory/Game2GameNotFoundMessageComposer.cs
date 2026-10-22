using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Directory;

/// <summary>Game2GameNotFoundMessageComposer — there is no such game</summary>
[Message(ServerMessages.Game2GameNotFoundMessageComposer)]
public sealed partial record Game2GameNotFoundMessageComposer;

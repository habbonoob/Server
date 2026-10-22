using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Directory;

/// <summary>Game2StartingGameFailedMessageComposer — the game could not be started</summary>
[Message(ServerMessages.Game2StartingGameFailedMessageComposer)]
public sealed partial record Game2StartingGameFailedMessageComposer(int Reason);

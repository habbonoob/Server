using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Directory;

/// <summary>Game2JoiningGameFailedMessageComposer — the player could not join</summary>
[Message(ServerMessages.Game2JoiningGameFailedMessageComposer)]
public sealed partial record Game2JoiningGameFailedMessageComposer(int Reason);

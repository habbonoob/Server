using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

/// <summary>Game2EnterArenaFailedMessageComposer — the player could not come into the arena</summary>
[Message(ServerMessages.Game2EnterArenaFailedMessageComposer)]
public sealed partial record Game2EnterArenaFailedMessageComposer(int Reason);

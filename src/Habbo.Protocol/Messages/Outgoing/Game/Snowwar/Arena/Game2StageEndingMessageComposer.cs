using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

/// <summary>Game2StageEndingMessageComposer — the round is over</summary>
[Message(ServerMessages.Game2StageEndingMessageComposer)]
public sealed partial record Game2StageEndingMessageComposer(int TimeToNextState);

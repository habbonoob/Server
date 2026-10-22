using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

/// <summary>Game2StageRunningMessageComposer — the round runs for so many seconds</summary>
[Message(ServerMessages.Game2StageRunningMessageComposer)]
public sealed partial record Game2StageRunningMessageComposer(int TimeToStageEnd);

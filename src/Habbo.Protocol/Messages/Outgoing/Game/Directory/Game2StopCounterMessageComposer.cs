using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Directory;

/// <summary>Game2StopCounterMessageComposer — the countdown stopped: too few players again</summary>
[Message(ServerMessages.Game2StopCounterMessageComposer)]
public sealed partial record Game2StopCounterMessageComposer;

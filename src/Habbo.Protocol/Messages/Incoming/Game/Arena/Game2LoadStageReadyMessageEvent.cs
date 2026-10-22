using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Arena;

/// <summary>Game2LoadStageReadyMessageEvent — the arena has loaded this far</summary>
[Incoming(ClientMessages.Game2LoadStageReadyMessageEvent)]
public sealed partial record Game2LoadStageReadyMessageEvent(int Percentage);

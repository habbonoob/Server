using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

/// <summary>Game2StageLoadMessageComposer — load the arena</summary>
[Message(ServerMessages.Game2StageLoadMessageComposer)]
public sealed partial record Game2StageLoadMessageComposer(int GameType);

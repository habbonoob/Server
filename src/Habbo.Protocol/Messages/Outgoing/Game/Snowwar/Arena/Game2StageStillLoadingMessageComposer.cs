using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

/// <summary>Game2StageStillLoadingMessageComposer — how far the players have loaded the arena</summary>
[Message(ServerMessages.Game2StageStillLoadingMessageComposer)]
public sealed partial record Game2StageStillLoadingMessageComposer(int Percentage, IReadOnlyList<int> FinishedPlayers);

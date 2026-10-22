using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Directory;

/// <summary>Game2StartCounterMessageComposer — the game starts in so many seconds</summary>
[Message(ServerMessages.Game2StartCounterMessageComposer)]
public sealed partial record Game2StartCounterMessageComposer(int CountDownLength);

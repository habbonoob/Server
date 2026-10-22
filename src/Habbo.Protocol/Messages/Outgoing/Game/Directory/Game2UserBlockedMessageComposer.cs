using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Directory;

/// <summary>Game2UserBlockedMessageComposer — the player may not play for so many seconds</summary>
[Message(ServerMessages.Game2UserBlockedMessageComposer)]
public sealed partial record Game2UserBlockedMessageComposer(int PlayerBlockLength);

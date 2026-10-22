using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Directory;

/// <summary>Game2UserLeftGameMessageComposer — somebody left the game</summary>
[Message(ServerMessages.Game2UserLeftGameMessageComposer)]
public sealed partial record Game2UserLeftGameMessageComposer(int UserId);

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Directory;

/// <summary>Game2LeaveGameMessageEvent — leave the game being waited for</summary>
[Incoming(ClientMessages.Game2LeaveGameMessageEvent)]
public sealed partial record Game2LeaveGameMessageEvent;

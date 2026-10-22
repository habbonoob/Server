using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Action;

/// <summary>KickUserMessageEvent</summary>
[Incoming(ClientMessages.KickUserMessageEvent)]
public sealed partial record KickUserMessageEvent(int UserId);

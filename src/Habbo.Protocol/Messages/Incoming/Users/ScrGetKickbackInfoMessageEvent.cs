using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>2059 ScrGetKickbackInfo — the club payday figures.</summary>
[Incoming(ClientMessages.ScrGetKickbackInfoMessageEvent)]
public sealed partial record ScrGetKickbackInfoMessageEvent;

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.GroupForums;

/// <summary>GetUnreadForumsCountMessageEvent</summary>
[Incoming(ClientMessages.GetUnreadForumsCountMessageEvent)]
public sealed partial record GetUnreadForumsCountMessageEvent;

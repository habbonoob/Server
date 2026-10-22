using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Action;

/// <summary>AmbassadorAlertMessageEvent</summary>
[Incoming(ClientMessages.AmbassadorAlertMessageEvent)]
public sealed partial record AmbassadorAlertMessageEvent(int UserId);

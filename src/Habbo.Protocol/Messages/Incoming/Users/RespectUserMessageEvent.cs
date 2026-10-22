using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>1465 RespectUser — give one of the day's respects to the user with this account id.</summary>
[Incoming(ClientMessages.RespectUserMessageEvent)]
public sealed partial record RespectUserMessageEvent(int UserId);

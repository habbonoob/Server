using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>1454 UnignoreUser.</summary>
[Incoming(ClientMessages.UnignoreUserMessageEvent)]
public sealed partial record UnignoreUserMessageEvent(int UserId);

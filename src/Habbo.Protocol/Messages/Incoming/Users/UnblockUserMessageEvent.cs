using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>3892 UnblockUser.</summary>
[Incoming(ClientMessages.UnblockUserMessageEvent)]
public sealed partial record UnblockUserMessageEvent(int UserId);

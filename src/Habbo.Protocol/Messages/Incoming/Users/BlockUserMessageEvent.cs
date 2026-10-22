using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>3611 BlockUser.</summary>
[Incoming(ClientMessages.BlockUserMessageEvent)]
public sealed partial record BlockUserMessageEvent(int UserId);

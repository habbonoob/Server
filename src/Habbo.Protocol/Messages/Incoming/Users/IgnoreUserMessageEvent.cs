using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>3619 IgnoreUser — mute someone's chat.</summary>
[Incoming(ClientMessages.IgnoreUserMessageEvent)]
public sealed partial record IgnoreUserMessageEvent(int UserId);

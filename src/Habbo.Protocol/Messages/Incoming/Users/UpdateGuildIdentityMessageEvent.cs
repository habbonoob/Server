using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>UpdateGuildIdentityMessageEvent</summary>
[Incoming(ClientMessages.UpdateGuildIdentityMessageEvent)]
public sealed partial record UpdateGuildIdentityMessageEvent(int GroupId, string Name, string Description);

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>UnblockGroupMemberMessageEvent</summary>
[Incoming(ClientMessages.UnblockGroupMemberMessageEvent)]
public sealed partial record UnblockGroupMemberMessageEvent(int GroupId, int UserId);

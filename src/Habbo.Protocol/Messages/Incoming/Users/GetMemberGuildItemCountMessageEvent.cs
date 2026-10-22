using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>GetMemberGuildItemCountMessageEvent — how much furni a member has in the home room, before a kick</summary>
[Incoming(ClientMessages.GetMemberGuildItemCountMessageEvent)]
public sealed partial record GetMemberGuildItemCountMessageEvent(int GroupId, int UserId);

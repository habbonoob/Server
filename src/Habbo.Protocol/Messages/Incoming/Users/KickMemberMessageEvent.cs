using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>KickMemberMessageEvent — take a member out (and keep them out); a member leaving kicks themselves</summary>
[Incoming(ClientMessages.KickMemberMessageEvent)]
public sealed partial record KickMemberMessageEvent(int GroupId, int UserId, bool Block);

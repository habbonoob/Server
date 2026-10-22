using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GuildMembershipRejectedMessageComposer</summary>
[Message(ServerMessages.GuildMembershipRejectedMessageComposer)]
public sealed partial record GuildMembershipRejectedMessageComposer(int GroupId, int UserId);

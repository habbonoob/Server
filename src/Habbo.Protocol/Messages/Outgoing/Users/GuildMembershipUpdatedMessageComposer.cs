using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GuildMembershipUpdatedMessageComposer</summary>
[Message(ServerMessages.GuildMembershipUpdatedMessageComposer)]
public sealed partial record GuildMembershipUpdatedMessageComposer(int GroupId, MemberData Data);

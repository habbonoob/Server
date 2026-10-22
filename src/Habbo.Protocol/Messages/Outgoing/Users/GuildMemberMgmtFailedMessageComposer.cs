using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GuildMemberMgmtFailedMessageComposer</summary>
[Message(ServerMessages.GuildMemberMgmtFailedMessageComposer)]
public sealed partial record GuildMemberMgmtFailedMessageComposer(int GroupId, int Reason);

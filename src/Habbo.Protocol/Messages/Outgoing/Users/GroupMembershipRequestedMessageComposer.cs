using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GroupMembershipRequestedMessageComposer</summary>
[Message(ServerMessages.GroupMembershipRequestedMessageComposer)]
public sealed partial record GroupMembershipRequestedMessageComposer(int GroupId, MemberData Requester);

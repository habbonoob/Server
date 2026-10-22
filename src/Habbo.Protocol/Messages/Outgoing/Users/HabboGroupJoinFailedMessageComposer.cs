using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>HabboGroupJoinFailedMessageComposer</summary>
[Message(ServerMessages.HabboGroupJoinFailedMessageComposer)]
public sealed partial record HabboGroupJoinFailedMessageComposer(int Reason);

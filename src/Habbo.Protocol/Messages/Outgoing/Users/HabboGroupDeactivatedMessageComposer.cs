using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>HabboGroupDeactivatedMessageComposer</summary>
[Message(ServerMessages.HabboGroupDeactivatedMessageComposer)]
public sealed partial record HabboGroupDeactivatedMessageComposer(int GroupId);

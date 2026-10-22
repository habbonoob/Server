using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.GroupForums;

/// <summary>UpdateThreadMessageComposer</summary>
[Message(ServerMessages.UpdateThreadMessageComposer)]
public sealed partial record UpdateThreadMessageComposer(int GroupId, ThreadData Thread);

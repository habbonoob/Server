using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.GroupForums;

/// <summary>PostThreadMessageComposer</summary>
[Message(ServerMessages.PostThreadMessageComposer)]
public sealed partial record PostThreadMessageComposer(int GroupId, ThreadData Thread);

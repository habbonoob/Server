using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.GroupForums;

/// <summary>UpdateThreadMessageEvent</summary>
[Incoming(ClientMessages.UpdateThreadMessageEvent)]
public sealed partial record UpdateThreadMessageEvent(int GroupId, int ThreadId, bool IsSticky, bool IsLocked);

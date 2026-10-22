using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.GroupForums;

/// <summary>GetThreadMessageEvent</summary>
[Incoming(ClientMessages.GetThreadMessageEvent)]
public sealed partial record GetThreadMessageEvent(int GroupId, int ThreadId);

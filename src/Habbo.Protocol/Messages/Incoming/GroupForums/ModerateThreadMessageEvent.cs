using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.GroupForums;

/// <summary>ModerateThreadMessageEvent</summary>
[Incoming(ClientMessages.ModerateThreadMessageEvent)]
public sealed partial record ModerateThreadMessageEvent(int GroupId, int ThreadId, int State);

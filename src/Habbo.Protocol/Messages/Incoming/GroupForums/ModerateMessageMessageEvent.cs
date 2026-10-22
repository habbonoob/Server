using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.GroupForums;

/// <summary>ModerateMessageMessageEvent</summary>
[Incoming(ClientMessages.ModerateMessageMessageEvent)]
public sealed partial record ModerateMessageMessageEvent(int GroupId, int ThreadId, int MessageId, int State);

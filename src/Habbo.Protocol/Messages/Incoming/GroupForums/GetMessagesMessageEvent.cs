using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.GroupForums;

/// <summary>GetMessagesMessageEvent</summary>
[Incoming(ClientMessages.GetMessagesMessageEvent)]
public sealed partial record GetMessagesMessageEvent(int GroupId, int ThreadId, int StartIndex, int Amount);

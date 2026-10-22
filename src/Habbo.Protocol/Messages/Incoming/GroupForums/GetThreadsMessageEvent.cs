using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.GroupForums;

/// <summary>GetThreadsMessageEvent</summary>
[Incoming(ClientMessages.GetThreadsMessageEvent)]
public sealed partial record GetThreadsMessageEvent(int GroupId, int StartIndex, int Amount);

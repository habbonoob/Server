using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.GroupForums;

/// <summary>PostMessageMessageEvent — a new thread (thread 0) or a reply</summary>
[Incoming(ClientMessages.PostMessageMessageEvent)]
public sealed partial record PostMessageMessageEvent(int GroupId, int ThreadId, string Subject, string Message);

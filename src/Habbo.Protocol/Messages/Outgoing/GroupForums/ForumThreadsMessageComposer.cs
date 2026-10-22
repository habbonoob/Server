using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.GroupForums;

/// <summary>ForumThreadsMessageComposer</summary>
[Message(ServerMessages.ForumThreadsMessageComposer)]
public sealed partial record ForumThreadsMessageComposer(int GroupId, int StartIndex, IReadOnlyList<ThreadData> Threads);

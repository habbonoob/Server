using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.GroupForums;

/// <summary>ThreadMessagesMessageComposer</summary>
[Message(ServerMessages.ThreadMessagesMessageComposer)]
public sealed partial record ThreadMessagesMessageComposer(int GroupId, int ThreadId, int StartIndex, IReadOnlyList<ForumMessageData> Messages);

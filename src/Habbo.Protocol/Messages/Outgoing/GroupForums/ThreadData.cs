using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.GroupForums;

/// <summary>ThreadData</summary>
[WireStruct]
public sealed partial record ThreadData(int ThreadId, int ThreadAuthorId, string ThreadAuthorName, string Header, bool IsSticky, bool IsLocked, int CreationTimeAsSecondsAgo, int MessageCount, int UnreadMessageCount, int LastMessageId, int LastMessageAuthorId, string LastMessageAuthorName, int LastMessageTimeAsSecondsAgo, byte State, int AdminId, string AdminName, int AdminOperationTimeAsSecondsAgo);

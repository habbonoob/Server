using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.GroupForums;

/// <summary>ForumData</summary>
[WireStruct]
public sealed partial record ForumData(int GroupId, string Name, string Description, string Icon, int TotalThreads, int LeaderboardScore, int TotalMessages, int UnreadMessages, int LastMessageId, int LastMessageAuthorId, string LastMessageAuthorName, int LastMessageTimeAsSecondsAgo);

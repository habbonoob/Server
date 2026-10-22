using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.GroupForums;

/// <summary>ForumMessageData</summary>
[WireStruct]
public sealed partial record ForumMessageData(int MessageId, int MessageIndex, int AuthorId, string AuthorName, string AuthorFigure, int CreationTimeAsSecondsAgo, string MessageText, byte State, int AdminId, string AdminName, int AdminOperationTimeAsSecondsAgo, int AuthorPostCount);

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>CloseIssueDefaultActionMessageEvent — close a bundle, sanctioning its reported user by default</summary>
[Incoming(ClientMessages.CloseIssueDefaultActionMessageEvent)]
public sealed partial record CloseIssueDefaultActionMessageEvent(int IssueId, IReadOnlyList<int> OtherIssueIds, int TopicId);

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>CloseIssuesMessageEvent</summary>
[Incoming(ClientMessages.CloseIssuesMessageEvent)]
public sealed partial record CloseIssuesMessageEvent(int CloseReason, IReadOnlyList<int> IssueIds);

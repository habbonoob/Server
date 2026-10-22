using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>ReleaseIssuesMessageEvent</summary>
[Incoming(ClientMessages.ReleaseIssuesMessageEvent)]
public sealed partial record ReleaseIssuesMessageEvent(IReadOnlyList<int> IssueIds);

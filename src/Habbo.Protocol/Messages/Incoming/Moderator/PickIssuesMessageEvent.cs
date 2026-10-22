using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>PickIssuesMessageEvent</summary>
[Incoming(ClientMessages.PickIssuesMessageEvent)]
public sealed partial record PickIssuesMessageEvent(IReadOnlyList<int> IssueIds, bool RetryEnabled, int RetryCount, string Message);

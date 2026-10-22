using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>IssuePickFailedMessageComposer</summary>
[Message(ServerMessages.IssuePickFailedMessageComposer)]
public sealed partial record IssuePickFailedMessageComposer(IReadOnlyList<PickFailedIssueData> Issues, bool RetryEnabled, int RetryCount);

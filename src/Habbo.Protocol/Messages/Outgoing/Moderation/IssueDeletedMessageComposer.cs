using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>IssueDeletedMessageComposer</summary>
[Message(ServerMessages.IssueDeletedMessageComposer)]
public sealed partial record IssueDeletedMessageComposer(string IssueId);

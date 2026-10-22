using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>IssueInfoMessageComposer</summary>
[Message(ServerMessages.IssueInfoMessageComposer)]
public sealed partial record IssueInfoMessageComposer(IssueMessageData Issue);

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>ModToolSanctionEvent — what a default sanction would be</summary>
[Incoming(ClientMessages.ModToolSanctionEvent)]
public sealed partial record ModToolSanctionEvent(int IssueId, int AccountId, int TopicId);

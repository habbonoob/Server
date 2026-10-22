using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>IssueCloseNotificationMessageComposer</summary>
[Message(ServerMessages.IssueCloseNotificationMessageComposer)]
public sealed partial record IssueCloseNotificationMessageComposer(int CloseReason, string MessageText);

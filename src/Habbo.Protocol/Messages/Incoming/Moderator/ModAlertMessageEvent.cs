using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>ModAlertMessageEvent</summary>
[Incoming(ClientMessages.ModAlertMessageEvent)]
public sealed partial record ModAlertMessageEvent(int UserId, string Message, int TopicId, int? IssueId);

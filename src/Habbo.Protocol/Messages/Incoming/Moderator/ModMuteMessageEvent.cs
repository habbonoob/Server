using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>ModMuteMessageEvent</summary>
[Incoming(ClientMessages.ModMuteMessageEvent)]
public sealed partial record ModMuteMessageEvent(int UserId, string Message, int TopicId, int? IssueId);

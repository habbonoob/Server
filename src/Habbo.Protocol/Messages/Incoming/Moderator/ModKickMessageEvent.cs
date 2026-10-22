using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>ModKickMessageEvent</summary>
[Incoming(ClientMessages.ModKickMessageEvent)]
public sealed partial record ModKickMessageEvent(int UserId, string Message, int TopicId, int? IssueId);

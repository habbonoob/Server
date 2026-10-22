using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>ModMessageMessageEvent</summary>
[Incoming(ClientMessages.ModMessageMessageEvent)]
public sealed partial record ModMessageMessageEvent(int UserId, string Message, string Unused1, string Unused2, int TopicId, int? IssueId);

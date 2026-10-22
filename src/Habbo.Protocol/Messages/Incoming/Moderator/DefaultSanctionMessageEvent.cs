using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>DefaultSanctionMessageEvent</summary>
[Incoming(ClientMessages.DefaultSanctionMessageEvent)]
public sealed partial record DefaultSanctionMessageEvent(int UserId, int TopicId, string Message, int? IssueId);

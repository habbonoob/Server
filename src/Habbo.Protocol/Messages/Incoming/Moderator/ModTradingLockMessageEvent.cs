using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>ModTradingLockMessageEvent</summary>
[Incoming(ClientMessages.ModTradingLockMessageEvent)]
public sealed partial record ModTradingLockMessageEvent(int UserId, string Message, int Minutes, int TopicId, int? IssueId);

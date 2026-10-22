using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>ModBanMessageEvent</summary>
[Incoming(ClientMessages.ModBanMessageEvent)]
public sealed partial record ModBanMessageEvent(int UserId, string Message, int TopicId, int SanctionTypeId, bool AvatarOnly, int? IssueId);

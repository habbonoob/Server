using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>2611 GetSelectedBadges — the badges a user wears, for their info stand.</summary>
[Incoming(ClientMessages.GetSelectedBadgesMessageEvent)]
public sealed partial record GetSelectedBadgesMessageEvent(int UserId);

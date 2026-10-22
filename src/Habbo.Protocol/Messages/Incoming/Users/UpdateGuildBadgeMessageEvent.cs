using Habbo.Protocol.Wire;
using Habbo.Protocol.Messages.Outgoing.Inventory.Badges;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>UpdateGuildBadgeMessageEvent</summary>
[Incoming(ClientMessages.UpdateGuildBadgeMessageEvent)]
public sealed partial record UpdateGuildBadgeMessageEvent(int GroupId, IReadOnlyList<int> BadgeData);

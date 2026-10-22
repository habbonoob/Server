using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Badges;

/// <summary>GetBadgeInfo (September 2026) — [badgeCode]: a badge display asks how rare its badge is.</summary>
[Incoming(ClientMessages.GetBadgeInfoEvent)]
public sealed partial record GetBadgeInfoEvent(string BadgeCode);

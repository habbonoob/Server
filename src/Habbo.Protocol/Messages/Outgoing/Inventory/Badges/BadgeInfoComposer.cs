using Habbo.Domain.Users;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Badges;

/// <summary>BadgeInfo (September 2026) — how many own a badge and how rare that makes it, for a badge display.</summary>
[Message(ServerMessages.BadgeInfoComposer)]
public sealed partial record BadgeInfoComposer(int BadgeId, string BadgeCode, int Owners, BadgeRarity Rarity);

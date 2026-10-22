using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Badges;

/// <summary>2788 BadgePointLimitsComposer.</summary>
[Message(ServerMessages.BadgePointLimitsComposer)]
public sealed partial record BadgePointLimitsComposer(IReadOnlyList<BadgePointLimitData> Achievements);

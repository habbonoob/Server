using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Badges;

/// <summary>997 GetBadgePointLimits — the achievement score each badge level is worth reaching.</summary>
[Incoming(ClientMessages.GetBadgePointLimitsEvent)]
public sealed partial record GetBadgePointLimitsEvent;

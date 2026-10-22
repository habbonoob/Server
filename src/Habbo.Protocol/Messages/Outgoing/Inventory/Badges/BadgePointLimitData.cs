using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Badges;

/// <summary>One achievement's levels in BadgePointLimitsComposer; the client prefixes "ACH_".</summary>
[WireStruct]
public sealed partial record BadgePointLimitData(string AchievementName, IReadOnlyList<BadgeLevelLimitData> Levels);

using Habbo.Domain.Users;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Notifications;

/// <summary>AchievementLevelUpData — a level of an achievement the player just reached; since September 2026 with how
/// many own its badge and how rare that makes it.</summary>
[WireStruct]
public sealed partial record AchievementLevelUpData(
    int Type,
    int Level,
    int BadgeId,
    string BadgeCode,
    int Points,
    int LevelRewardPoints,
    int LevelRewardPointType,
    int BonusPoints,
    int AchievementId,
    string RemovedBadgeCode,
    string Category,
    bool ShowDialogToUser,
    [property: WireSince(ClientReleases.September2026)] int BadgeOwners,
    [property: WireSince(ClientReleases.September2026)] BadgeRarity BadgeRarity);

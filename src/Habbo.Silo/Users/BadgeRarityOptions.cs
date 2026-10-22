using Habbo.Domain.Users;

namespace Habbo.Silo.Users;

/// <summary>How many owners each badge rarity allows at most (config section "BadgeRarity"); a badge with more owners
/// than the uncommon limit is common. The defaults are the hotel's: its leaderboard texts give the unique, legendary,
/// epic, rare and uncommon limits, and the live hotel's badges put four and five owners in mythical.</summary>
public sealed class BadgeRarityOptions
{
    public int UniqueMaxOwners { get; set; } = 1;
    public int LegendaryMaxOwners { get; set; } = 3;
    public int MythicalMaxOwners { get; set; } = 5;
    public int EpicMaxOwners { get; set; } = 10;
    public int RareMaxOwners { get; set; } = 50;
    public int UncommonMaxOwners { get; set; } = 200;

    /// <summary>The most owners a badge is said to have: the client shows anything from it as "1000+".</summary>
    public const int ShownOwnersMax = 1000;

    /// <summary>The rarity of a badge with this many owners.</summary>
    public BadgeRarity Of(int owners) =>
        owners <= UniqueMaxOwners ? BadgeRarity.Unique
        : owners <= LegendaryMaxOwners ? BadgeRarity.Legendary
        : owners <= MythicalMaxOwners ? BadgeRarity.Mythical
        : owners <= EpicMaxOwners ? BadgeRarity.Epic
        : owners <= RareMaxOwners ? BadgeRarity.Rare
        : owners <= UncommonMaxOwners ? BadgeRarity.Uncommon
        : BadgeRarity.Common;

    /// <summary>The owner count as the hotel tells it.</summary>
    public static int Shown(int owners) => Math.Min(owners, ShownOwnersMax);
}

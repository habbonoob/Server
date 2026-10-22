namespace Habbo.Domain.Users;

/// <summary>How few players own a badge, as the client's badge rarity tiers number it (September 2026). The hotel's
/// own texts say who gets which: one player a unique badge, two or three a legendary one, and so on up to common.</summary>
public enum BadgeRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Mythical = 4,
    Legendary = 5,
    Unique = 6,
}

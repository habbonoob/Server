using Habbo.Domain.Users;
using Habbo.Silo.Users;

namespace Habbo.Silo.Tests;

public class BadgeRarityTests
{
    /// <summary>Owner counts and rarities the live hotel gave for its badges (GetBadgeInfo, October 2026).</summary>
    [Theory]
    [InlineData(0, BadgeRarity.Unique)]
    [InlineData(1, BadgeRarity.Unique)]
    [InlineData(2, BadgeRarity.Legendary)]
    [InlineData(3, BadgeRarity.Legendary)]
    [InlineData(4, BadgeRarity.Mythical)]
    [InlineData(5, BadgeRarity.Mythical)]
    [InlineData(9, BadgeRarity.Epic)]
    [InlineData(10, BadgeRarity.Epic)]
    [InlineData(13, BadgeRarity.Rare)]
    [InlineData(49, BadgeRarity.Rare)]
    [InlineData(52, BadgeRarity.Uncommon)]
    [InlineData(197, BadgeRarity.Uncommon)]
    [InlineData(209, BadgeRarity.Common)]
    [InlineData(5000, BadgeRarity.Common)]
    public void ABadgesRarityIsHowFewOwnIt(int owners, BadgeRarity rarity) =>
        Assert.Equal(rarity, new BadgeRarityOptions().Of(owners));

    [Theory]
    [InlineData(999, 999)]
    [InlineData(1000, 1000)]
    [InlineData(25000, 1000)]
    public void OwnersAreToldUpToTheClientsOwnLimit(int owners, int shown) =>
        Assert.Equal(shown, BadgeRarityOptions.Shown(owners));
}

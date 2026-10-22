using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Catalog;

/// <summary>What the catalog shows around its pages: when pages stop being sold, the next limited
/// edition, the bonus rare, and the seasonal calendar. Every call answers the player who made it.</summary>
public interface ICatalogExtrasService
{
    Task SendPageExpirationAsync(PlayerData player, string pageName);

    Task SendEarliestExpiringPageAsync(PlayerData player);

    Task SendLimitedOfferAppearingNextAsync(PlayerData player);

    Task SendBonusRareInfoAsync(PlayerData player);

    Task SendSeasonalCalendarOfferAsync(PlayerData player);
}

using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Subscriptions;

/// <summary>Habbo Club as the catalog sells it: its offers, the discount offered to a membership about
/// to run out, and the monthly gifts. Every call answers the player who made it.</summary>
public interface IClubService
{
    Task SendOffersAsync(PlayerData player, int source);

    /// <summary>The discounted extension, when the membership is close enough to its end.</summary>
    Task SendExtendOfferAsync(PlayerData player, bool vip);

    /// <summary>Buys the discounted extension.</summary>
    Task<PurchaseResult> PurchaseExtensionAsync(PlayerData player, int offerId, bool vip);

    Task SendGiftInfoAsync(PlayerData player);

    /// <summary>Reminds a member of the club gifts they have earned and not yet chosen, if any.</summary>
    Task NotifyGiftsAsync(PlayerData player);

    /// <summary>Whether the club can be bought by text message, and where.</summary>
    Task SendDirectBuyAvailabilityAsync(PlayerData player, int lengthInDays);

    /// <summary>Takes one of the monthly gifts, by its product code.</summary>
    Task SelectGiftAsync(PlayerData player, string productCode);
}

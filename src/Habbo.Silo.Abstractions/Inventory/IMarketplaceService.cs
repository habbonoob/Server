using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Inventory;

/// <summary>The marketplace. Every call answers the player who made it.</summary>
public interface IMarketplaceService
{
    Task SendConfigurationAsync(PlayerData player);

    Task SendCanMakeOfferAsync(PlayerData player);

    Task BuyTokensAsync(PlayerData player);

    /// <summary>Puts the items up for sale, each its own offer at the price: all of them, or none.</summary>
    Task MakeOfferAsync(PlayerData player, int price, int furniType, IReadOnlyList<int> itemIds);

    /// <summary>A search; limited editions of a type listed together, or each number on its own. A November 2026 client
    /// says which offers (0 all, 1 collectibles, the NFTs, 2 furni); an older one (null) is never shown an NFT.</summary>
    Task SendOffersAsync(PlayerData player, int minPrice, int maxPrice, string searchQuery, int sortOrder, bool combineUniques,
        int? offerSource = null);

    /// <summary>The player's own offers: those of one kind (open, sold, expired), or with no kind, those a client
    /// before September 2026 lists (open, expired, and sales not yet collected).</summary>
    Task SendOwnOffersAsync(PlayerData player, int? category = null);

    /// <summary>Takes every open offer back.</summary>
    Task CancelAllAsync(PlayerData player);

    /// <summary>Clears the sold, or the expired, offers from the player's list.</summary>
    Task ClearHistoryAsync(PlayerData player, int category);

    Task BuyAsync(PlayerData player, int offerId);

    Task CancelAsync(PlayerData player, int offerId);

    Task RedeemCreditsAsync(PlayerData player);

    /// <summary>An item type's sales; for a poster, of one design (its poster id) when it is named.</summary>
    Task SendItemStatsAsync(PlayerData player, int furniType, int classId, string? posterId = null);

    /// <summary>Puts an NFT up for sale for emeralds, as its own offer (November 2026).</summary>
    Task MakeNftOfferAsync(PlayerData player, int nftFurniType, int itemId, int price);

    /// <summary>Buys the cheapest NFT on sale of a product (its productdata code) for emeralds, if it costs no more than the
    /// price the buyer was shown (November 2026).</summary>
    Task BuyNftAsync(PlayerData player, string productCode, int price);

    /// <summary>An NFT product's sales (November 2026).</summary>
    Task SendNftItemStatsAsync(PlayerData player, string productCode);
}

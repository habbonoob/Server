using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Catalog;

/// <summary>Targeted offers. Every call answers the player who made it.</summary>
public interface ITargetedOfferService
{
    Task SendAsync(PlayerData player, int offerId);

    Task SendNextAsync(PlayerData player);

    Task<PurchaseResult> PurchaseAsync(PlayerData player, int offerId, int quantity);

    /// <summary>How the client shows the offer now; it comes back that way.</summary>
    Task SetStateAsync(PlayerData player, int offerId, int trackingState);
}

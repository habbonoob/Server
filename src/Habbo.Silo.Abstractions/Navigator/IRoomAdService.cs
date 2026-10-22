using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Navigator;

/// <summary>Room ads: buying a room's event from the catalog, or more time for the one it has.</summary>
public interface IRoomAdService
{
    /// <summary>The rooms the player may promote.</summary>
    Task SendPurchaseInfoAsync(PlayerData player);

    Task<PurchaseResult> PurchaseAsync(PlayerData player, int offerId, int roomId, string name, bool extended, string description, int categoryId);

    Task RecordPurchaseInitiatedAsync(PlayerData player);
}

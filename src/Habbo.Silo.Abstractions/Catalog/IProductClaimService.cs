using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Catalog;

/// <summary>Products a player may claim once for free.</summary>
public interface IProductClaimService
{
    Task SendHasClaimedAsync(PlayerData player, string claimId);

    Task ClaimAsync(PlayerData player, string claimId);
}

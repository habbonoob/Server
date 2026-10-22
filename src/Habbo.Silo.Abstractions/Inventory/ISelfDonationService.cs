using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Inventory;

/// <summary>The sandbox's tool for giving oneself furniture.</summary>
public interface ISelfDonationService
{
    Task DonateAsync(PlayerData player, bool isWallItem, int typeId, string posterId, int amount);
}

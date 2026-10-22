using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Inventory;

/// <summary>The Furni-Matic. Every call answers the player who made it.</summary>
public interface IRecyclerService
{
    Task SendStatusAsync(PlayerData player);

    Task SendPrizesAsync(PlayerData player);

    Task RecycleAsync(PlayerData player, IReadOnlyList<int> itemIds);
}

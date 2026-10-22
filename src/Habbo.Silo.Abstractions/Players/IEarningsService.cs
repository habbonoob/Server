namespace Habbo.Silo.Abstractions.Players;

/// <summary>The earnings window: what a player has earned and not yet claimed, and their credit vault.</summary>
public interface IEarningsService
{
    Task SendStatusAsync(PlayerData player);

    /// <summary>Claims one category, or every category at once.</summary>
    Task ClaimAsync(PlayerData player, byte category);

    Task SendVaultStatusAsync(PlayerData player);

    Task WithdrawVaultAsync(PlayerData player);

    /// <summary>Puts credits or duckets into a player's earnings, and tells them if they are online.</summary>
    Task EarnAsync(Guid playerId, byte category, byte rewardType, int amount);

    /// <summary>Tells the player, if they are online, that something waits in their earnings: for earnings written
    /// in a transaction of the caller's own, once it has committed.</summary>
    Task NotifyAsync(Guid playerId, byte category);
}

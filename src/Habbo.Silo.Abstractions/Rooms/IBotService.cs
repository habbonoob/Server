using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>The bots players own. Every call answers the player who made it.</summary>
public interface IBotService
{
    /// <summary>The bots the player owns and has not put down anywhere.</summary>
    Task SendInventoryAsync(PlayerData player);
}

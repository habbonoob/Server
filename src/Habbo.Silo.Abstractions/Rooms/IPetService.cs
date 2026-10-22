using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>Pets outside the rooms they are in. Every call answers the player who made it.</summary>
public interface IPetService
{
    /// <summary>The pets the player owns and has not put down anywhere.</summary>
    Task SendInventoryAsync(PlayerData player);

    /// <summary>The palettes a pet offer sells, by the offer's product code.</summary>
    Task SendSellablePalettesAsync(PlayerData player, string productCode);

    /// <summary>Whether a name would be accepted, for a thing of this kind.</summary>
    Task ApproveNameAsync(PlayerData player, string name, int type);
}

using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Hotel;

/// <summary>The hotel's opening hours and maintenance breaks.</summary>
public interface IHotelAvailabilityService
{
    HotelAvailability GetAvailability();

    /// <summary>Whether the player may be in the hotel while it is closed.</summary>
    bool MayEnterWhileClosed(PlayerData player);

    /// <summary>The longest warning the hotel gives before it closes.</summary>
    int LongestWarningMinutes { get; }
}

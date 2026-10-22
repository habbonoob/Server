using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Catalog;

/// <summary>The running campaign's calendar and the doors each player opens.</summary>
public interface ICampaignCalendarService
{
    /// <summary>The running calendar and the player's doors; nothing when no campaign is running.</summary>
    Task SendAsync(PlayerData player);

    Task OpenDoorAsync(PlayerData player, string campaignName, int day, bool asStaff);
}

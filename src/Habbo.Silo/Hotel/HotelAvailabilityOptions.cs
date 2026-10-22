using Habbo.Domain.Users;

namespace Habbo.Silo.Hotel;

/// <summary>When the hotel is open (config section "Availability"). It is read afresh on every check,
/// so changing the file reschedules the hotel without a restart.</summary>
public sealed class HotelAvailabilityOptions
{
    /// <summary>Opening hours as "HH:mm" in <see cref="TimeZone"/>. Both empty, or the same, keeps the hotel
    /// open around the clock; a closing time before the opening time runs past midnight.</summary>
    public string OpensAt { get; set; } = string.Empty;
    public string ClosesAt { get; set; } = string.Empty;

    /// <summary>The time zone the hours are kept in; empty is the server's own.</summary>
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>How many minutes before closing, or before maintenance, everyone is warned.</summary>
    public List<int> WarningMinutes { get; set; } = [];

    /// <summary>Whether closing sends everyone still in the hotel away.</summary>
    public bool ThrowOutAtClose { get; set; }

    /// <summary>A planned maintenance break, if one is coming.</summary>
    public HotelMaintenanceOptions Maintenance { get; set; } = new();
}

using Habbo.Domain.Users;

namespace Habbo.Silo.Campaigns;

/// <summary>How campaign calendars open (config section "CampaignCalendar").</summary>
public sealed class CampaignCalendarOptions
{
    /// <summary>How many days a door stays openable after its own; older doors not opened are missed.</summary>
    public int CatchUpDays { get; set; }
}

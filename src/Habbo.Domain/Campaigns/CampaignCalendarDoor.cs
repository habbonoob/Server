namespace Habbo.Domain.Campaigns;

/// <summary>A door of a campaign calendar: the furniture behind it, and how the calendar shows it.</summary>
public sealed class CampaignCalendarDoor
{
    public string CampaignName { get; set; } = string.Empty;

    /// <summary>Which day the door belongs to, from 0.</summary>
    public int Day { get; set; }

    /// <summary>The furniture handed over, by class name.</summary>
    public string FurniClassName { get; set; } = string.Empty;

    /// <summary>The product the calendar names; usually the furniture's own.</summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>A picture from the image library shown instead of the furniture's icon; empty for the icon.</summary>
    public string CustomImage { get; set; } = string.Empty;
}

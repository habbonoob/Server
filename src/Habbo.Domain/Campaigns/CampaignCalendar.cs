namespace Habbo.Domain.Campaigns;

/// <summary>A campaign's calendar: one door a day from the day it starts, each hiding a piece of furniture.</summary>
public sealed class CampaignCalendar
{
    public const int NameMaxLength = 64;

    /// <summary>The campaign's name, which the client sends back when it opens a door.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The calendar's picture, from the image library.</summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>The day of the first door; door n opens n days later.</summary>
    public DateOnly StartsOn { get; set; }

    public List<CampaignCalendarDoor> Doors { get; set; } = [];
}

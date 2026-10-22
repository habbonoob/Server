namespace Habbo.Domain.Campaigns;

/// <summary>A door a player has opened.</summary>
public sealed class CampaignCalendarOpening
{
    public Guid UserId { get; set; }

    public string CampaignName { get; set; } = string.Empty;

    public int Day { get; set; }

    public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.UtcNow;
}

namespace Habbo.Silo.Hotel;

/// <summary>A planned maintenance break: the hotel closes when it starts and opens again when it is over.</summary>
public sealed class HotelMaintenanceOptions
{
    /// <summary>When the break starts; none when no break is planned.</summary>
    public DateTimeOffset? StartsAt { get; set; }

    public int DurationMinutes { get; set; }
}

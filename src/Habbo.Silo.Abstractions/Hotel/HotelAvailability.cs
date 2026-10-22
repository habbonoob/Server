namespace Habbo.Silo.Abstractions.Hotel;

/// <summary>Whether the hotel is open right now and, when it is closed, the local time it opens again and
/// whether a maintenance break closed it. Closing or maintenance still to come carries the minutes left until it.</summary>
public sealed record HotelAvailability(
    bool IsOpen,
    bool InMaintenance,
    int ReopenHour,
    int ReopenMinute,
    int? MinutesUntilClosing,
    int? MinutesUntilMaintenance,
    int MaintenanceMinutes)
{
    /// <summary>Whether the hotel is warning of its closing or of maintenance.</summary>
    public bool IsShuttingDown(int warningMinutes) =>
        MinutesUntilClosing <= warningMinutes || MinutesUntilMaintenance <= warningMinutes;
}

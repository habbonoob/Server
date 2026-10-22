using Habbo.Silo.Abstractions.Permissions;
using System.Globalization;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Players;
using Microsoft.Extensions.Options;
using Habbo.Silo.Permissions;

namespace Habbo.Silo.Hotel;

/// <summary>Works out from the opening hours and any planned maintenance whether the hotel is open,
/// when it opens again, and how long until it closes.</summary>
public sealed class HotelAvailabilityService(IOptionsMonitor<HotelAvailabilityOptions> options, PermissionService permissions) : IHotelAvailabilityService
{
    private const int MinutesPerDay = 24 * 60;

    public int LongestWarningMinutes => options.CurrentValue.WarningMinutes.DefaultIfEmpty(0).Max();

    public bool MayEnterWhileClosed(PlayerData player) => permissions.Has(player, PermissionNames.HotelEnterWhileClosed);

    public HotelAvailability GetAvailability()
    {
        var rules = options.CurrentValue;
        var zone = rules.TimeZone.Length == 0 ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(rules.TimeZone);
        var now = DateTimeOffset.UtcNow;
        var hours = OpeningHours(rules);

        // a maintenance break closes the hotel until it is over, or until opening time if that is later
        var maintenance = rules.Maintenance;
        if (maintenance.StartsAt is { } start && maintenance.DurationMinutes > 0)
        {
            var end = start.AddMinutes(maintenance.DurationMinutes);
            if (now >= start && now < end)
            {
                var reopen = MinuteOfDay(TimeZoneInfo.ConvertTime(end, zone));
                if (hours is { } open && !IsWithin(open, reopen))
                    reopen = open.Opens;
                return new HotelAvailability(false, true, reopen / 60, reopen % 60, null, null, maintenance.DurationMinutes);
            }
        }

        int? untilMaintenance = maintenance.StartsAt is { } upcoming && upcoming > now && maintenance.DurationMinutes > 0
            ? (int)Math.Ceiling((upcoming - now).TotalMinutes)
            : null;
        if (hours is not { } schedule)
            return new HotelAvailability(true, false, 0, 0, null, untilMaintenance, maintenance.DurationMinutes);

        var minute = MinuteOfDay(TimeZoneInfo.ConvertTime(now, zone));
        if (!IsWithin(schedule, minute))
            return new HotelAvailability(false, false, schedule.Opens / 60, schedule.Opens % 60, null, null, maintenance.DurationMinutes);

        var untilClosing = (schedule.Closes - minute + MinutesPerDay) % MinutesPerDay;
        return new HotelAvailability(true, false, schedule.Opens / 60, schedule.Opens % 60, untilClosing, untilMaintenance, maintenance.DurationMinutes);
    }

    /// <summary>The hours as minutes of the day, or none when the hotel never closes.</summary>
    private static (int Opens, int Closes)? OpeningHours(HotelAvailabilityOptions rules)
    {
        if (rules.OpensAt.Length == 0 || rules.ClosesAt.Length == 0)
            return null;

        var opens = MinuteOfDay(TimeOnly.ParseExact(rules.OpensAt, "HH:mm", CultureInfo.InvariantCulture));
        var closes = MinuteOfDay(TimeOnly.ParseExact(rules.ClosesAt, "HH:mm", CultureInfo.InvariantCulture));
        return opens == closes ? null : (opens, closes);
    }

    private static bool IsWithin((int Opens, int Closes) hours, int minute) =>
        hours.Opens < hours.Closes
            ? minute >= hours.Opens && minute < hours.Closes
            : minute >= hours.Opens || minute < hours.Closes;

    private static int MinuteOfDay(DateTimeOffset moment) => moment.Hour * 60 + moment.Minute;

    private static int MinuteOfDay(TimeOnly moment) => moment.Hour * 60 + moment.Minute;
}

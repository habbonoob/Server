using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Hotel;

/// <summary>Resolves which timing code a landing-view schedule is showing right now.</summary>
public interface ITimingCodeService
{
    /// <summary>The code running for the schedule, or empty when nothing is scheduled for it —
    /// which the client reads as "draw the default".</summary>
    Task<string> GetCurrentCodeAsync(string schedulingKey, CancellationToken ct = default);
}

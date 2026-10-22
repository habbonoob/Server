using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Subscriptions;

/// <summary>A subscription as the client is told about it, already worked out against now.</summary>
[GenerateSerializer]
public partial record SubscriptionView(
    [property: Id(0)] string ProductName,
    [property: Id(1)] int DaysToPeriodEnd,
    [property: Id(2)] int MemberPeriods,
    [property: Id(3)] int PeriodsSubscribedAhead,
    [property: Id(4)] bool HasEverBeenMember,
    [property: Id(5)] bool IsVip,
    [property: Id(6)] int PastClubDays,
    [property: Id(7)] int PastVipDays,
    [property: Id(8)] int MinutesUntilExpiration,
    [property: Id(9)] int MinutesSinceLastModified);

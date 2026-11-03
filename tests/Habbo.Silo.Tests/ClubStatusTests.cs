using Habbo.Domain.Subscriptions;
using Habbo.Silo.Subscriptions;

namespace Habbo.Silo.Tests;

public class ClubStatusTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    private static Subscription Club(TimeSpan left) => new()
    {
        ProductName = SubscriptionProducts.HabboClub,
        ExpiresAt = Now + left,
        MemberPeriods = 18,
        LastModifiedAt = Now,
    };

    // A 31-day box on habbo.com (October 2026), as the client was told it while the minutes ran down:
    // 31 days and 0 periods ahead throughout.
    [Theory]
    [InlineData(44639)]
    [InlineData(44638)]
    [InlineData(44632)]
    [InlineData(44620)]
    public void ANewBoxIs31DaysWithNoPeriodsAhead(int minutesLeft)
    {
        var status = SubscriptionService.ClubStatus(Club(TimeSpan.FromMinutes(minutesLeft) + TimeSpan.FromSeconds(30)), Now);

        Assert.Equal(31, status.DaysToPeriodEnd);
        Assert.Equal(0, status.PeriodsSubscribedAhead);
        Assert.Equal(minutesLeft, status.MinutesUntilExpiration);
    }

    [Theory]
    [InlineData(31 * 24, 31, 0)]
    [InlineData(32 * 24, 1, 1)]
    [InlineData(62 * 24, 31, 1)]
    [InlineData(93 * 24, 31, 2)]
    [InlineData(93 * 24 - 1, 31, 2)]
    [InlineData(12, 1, 0)]
    public void DaysRunOneTo31InThePeriodAndTheRestArePeriodsAhead(int hoursLeft, int days, int ahead)
    {
        var status = SubscriptionService.ClubStatus(Club(TimeSpan.FromHours(hoursLeft)), Now);

        Assert.Equal(days, status.DaysToPeriodEnd);
        Assert.Equal(ahead, status.PeriodsSubscribedAhead);
    }

    [Fact]
    public void TheClientsTotalIsEveryDayBegun()
    {
        // three months bought, a minute later: the client adds periods * 31 + days
        var status = SubscriptionService.ClubStatus(Club(TimeSpan.FromDays(93) - TimeSpan.FromMinutes(1)), Now);

        Assert.Equal(93, status.PeriodsSubscribedAhead * SubscriptionService.DaysPerPeriod + status.DaysToPeriodEnd);
    }

    [Fact]
    public void ALapsedMembershipHasNothingLeft()
    {
        var status = SubscriptionService.ClubStatus(Club(TimeSpan.FromDays(-3)), Now);

        Assert.Equal((0, 0, 0, false), (status.DaysToPeriodEnd, status.PeriodsSubscribedAhead, status.MinutesUntilExpiration, status.IsVip));
        Assert.True(status.HasEverBeenMember);
    }
}

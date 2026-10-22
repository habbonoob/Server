using Habbo.Silo.Users;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Tests;

public class CurrencySeasonTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static CurrencyService Service(params CurrencySeason[] seasons) =>
        new(null!, null!, null!, Options.Create(new CurrencyOptions { Seasons = seasons.ToList() }));

    [Fact]
    public void ASeasonRunsFromItsStartUntilItsEnd()
    {
        var season = new CurrencySeason { Type = 101, StartsAt = Start, EndsAt = End };

        Assert.False(season.IsRunning(Start.AddSeconds(-1)));
        Assert.True(season.IsRunning(Start));
        Assert.False(season.IsRunning(End));
    }

    [Fact]
    public void ASeasonalCurrencyIsPaidOnlyWhileASeasonRuns()
    {
        var service = Service(new CurrencySeason { Type = 101, StartsAt = Start, EndsAt = End });

        Assert.True(service.IsPayable(101, Start.AddDays(10)));
        Assert.False(service.IsPayable(101, End.AddDays(1)));
        Assert.True(service.IsPayable(0, End.AddDays(1)));
    }
}

using System.Globalization;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Users;

/// <summary>Giving players currency: credits or any activity-point currency, by the name the hotel calls it.</summary>
public sealed class CurrencyService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    IClientVariables clientVariables,
    IOptions<CurrencyOptions> options)
{
    /// <summary>What <see cref="ResolveAsync"/> answers for credits, which are not an activity point.</summary>
    public const int Credits = -1;

    private const string CreditsName = "credits";

    /// <summary>The variable naming a seasonal currency's type, as the client reads it ("stars" for 101).</summary>
    private const string SeasonalNameVariable = "seasonalcurrency.id.";

    /// <summary>The type of a currency named as the hotel names it: credits, a currency's own name (duckets,
    /// diamonds), a seasonal currency's (stars), or its number; null for none.</summary>
    public async Task<int?> ResolveAsync(string name)
    {
        name = name.Trim().ToLowerInvariant();
        if (name == CreditsName)
            return Credits;
        if (int.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            return number;

        await using var db = await dbFactory.CreateDbContextAsync();
        var types = await db.CurrencyTypes.AsNoTracking().Select(t => new { t.Type, t.Name }).ToListAsync();
        if (types.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) is { } named)
            return named.Type;

        foreach (var type in types)
        {
            var seasonal = await clientVariables.GetAsync(SeasonalNameVariable + type.Type.ToString(CultureInfo.InvariantCulture));
            if (string.Equals(seasonal, name, StringComparison.OrdinalIgnoreCase))
                return type.Type;
        }

        return null;
    }

    /// <summary>Gives (or with a negative amount, takes) a currency; the player is told if they are online.
    /// Their new balance.</summary>
    public async Task<int> GiveAsync(Guid playerId, int type, int amount, string reason)
    {
        var player = grains.GetGrain<IPlayerGrain>(playerId);
        return type == Credits
            ? await player.AddCreditsAsync(amount, reason)
            : await player.AddActivityPointsAsync(type, amount, reason);
    }

    private DateTimeOffset? _happyHourUntil;

    /// <summary>Starts a happy hour of so many minutes, or ends the one running (minutes 0); when it runs until.</summary>
    public DateTimeOffset? SetHappyHour(int minutes)
    {
        _happyHourUntil = minutes > 0 ? DateTimeOffset.UtcNow.AddMinutes(minutes) : null;
        return _happyHourUntil;
    }

    /// <summary>How many times over the payouts are now: the happy hour's while one runs, else once.</summary>
    public int PayoutMultiplier(DateTimeOffset now) =>
        _happyHourUntil is { } until && now < until ? Math.Max(1, options.Value.HappyHourMultiplier) : 1;

    /// <summary>Whether a currency is seasonal, and if so whether a season of it runs now.</summary>
    public bool IsPayable(int type, DateTimeOffset now)
    {
        var seasons = options.Value.Seasons.Where(s => s.Type == type).ToList();
        return seasons.Count == 0 || seasons.Any(s => s.IsRunning(now));
    }
}

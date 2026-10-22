using Habbo.Domain.Chat;
using Habbo.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The account's reference data: the hotel's currencies and the chat bubbles.</summary>
public static partial class DbSeeder
{
    /// <summary>One row of seed/currencies.json.</summary>
    private sealed record SeedCurrency(int Type, string Name, int InitialAmount);

    /// <summary>seed/currencies.json — the currencies the hotel runs. Existing rows keep whatever
    /// the hotel changed them to.</summary>
    private static async Task SeedCurrenciesAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        const string file = "currencies.json";
        var currencies = await seedFiles.ReadJsonAsync<List<SeedCurrency>>(file, ct);
        if (currencies is null)
            return;

        var known = await db.CurrencyTypes.Select(t => t.Type).ToListAsync(ct);
        var added = 0;
        foreach (var currency in currencies.Where(c => !known.Contains(c.Type)))
        {
            db.CurrencyTypes.Add(new CurrencyType { Type = currency.Type, Name = currency.Name, InitialAmount = currency.InitialAmount });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} currency type(s) from {File}.", added, file);
        }
    }

    /// <summary>One style of seed/chat_styles.json, which is the client's own chatstyles_xml.</summary>
    private sealed record SeedChatStyle(
        int Id, string AssetId, bool SystemStyle, bool Purchasable, bool HcOnly, bool StaffOverrideable, bool AmbassadorOnly);

    /// <summary>seed/chat_styles.json — refreshed rather than only added, because it has to say
    /// exactly what the client's library says or the two disagree about what a player may pick.</summary>
    private static async Task SeedChatStylesAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        const string file = "chat_styles.json";
        var styles = await seedFiles.ReadJsonAsync<List<SeedChatStyle>>(file, ct);
        if (styles is null)
            return;

        var stored = await db.ChatStyles.ToDictionaryAsync(s => s.Id, ct);
        foreach (var seed in styles)
        {
            if (!stored.TryGetValue(seed.Id, out var style))
            {
                style = new ChatStyle { Id = seed.Id };
                db.ChatStyles.Add(style);
            }

            style.AssetId = seed.AssetId;
            style.SystemStyle = seed.SystemStyle;
            style.Purchasable = seed.Purchasable;
            style.HcOnly = seed.HcOnly;
            style.StaffOverrideable = seed.StaffOverrideable;
            style.AmbassadorOnly = seed.AmbassadorOnly;
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded chat styles from {File}.", file);
        }
    }
}

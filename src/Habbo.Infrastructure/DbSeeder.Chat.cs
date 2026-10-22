using Habbo.Domain.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The hotel's word filter.</summary>
public static partial class DbSeeder
{
    private sealed record SeedFilteredWord(string Word, string Replacement);

    /// <summary>seed/word_filter.json — words the filter replaces. Existing words keep the hotel's replacement.</summary>
    private static async Task SeedWordFilterAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var words = await seedFiles.ReadJsonAsync<List<SeedFilteredWord>>("word_filter.json", ct) ?? [];
        var known = await db.FilteredWords.Select(w => w.Word).ToListAsync(ct);
        var added = 0;
        foreach (var word in words.Where(w => !known.Contains(w.Word.ToLowerInvariant())))
        {
            db.FilteredWords.Add(new FilteredWord { Word = word.Word.ToLowerInvariant(), Replacement = word.Replacement ?? string.Empty });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} filtered word(s).", added);
        }
    }
}

using Habbo.Domain.Recycler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The Furni-Matic's prize levels.</summary>
public static partial class DbSeeder
{
    private sealed record SeedRecycler(List<SeedRecyclerLevel>? Levels);

    private sealed record SeedRecyclerLevel(int Id, int ProbabilityDenominator, List<string> Prizes);

    /// <summary>seed/recycler.json, seeded while the hotel has no prize levels.</summary>
    private static async Task SeedRecyclerAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        if (await db.RecyclerPrizeLevels.AnyAsync(ct) || await seedFiles.ReadJsonAsync<SeedRecycler>("recycler.json", ct) is not { } seed)
            return;

        var definitions = await db.ItemDefinitions.ToDictionaryAsync(d => d.ClassName, d => d.Id, ct);
        foreach (var level in seed.Levels ?? [])
        {
            db.RecyclerPrizeLevels.Add(new RecyclerPrizeLevel
            {
                Id = level.Id,
                ProbabilityDenominator = level.ProbabilityDenominator,
                Prizes = level.Prizes.Where(definitions.ContainsKey).Select(name => new RecyclerPrize { DefinitionId = definitions[name] }).ToList(),
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} recycler prize level(s).", seed.Levels?.Count ?? 0);
    }
}

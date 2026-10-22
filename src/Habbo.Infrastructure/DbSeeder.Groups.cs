using Habbo.Domain.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The group editor's pieces: Habbo's badge parts and colours, by Habbo's own ids.</summary>
public static partial class DbSeeder
{
    private sealed record SeedGroupBadgePart(int Id, GroupBadgePartKind Kind, string FileName, string MaskFileName);

    private sealed record SeedGroupColor(int Id, GroupColorKind Kind, string Hex);

    private sealed record SeedGroupBadgeParts(List<SeedGroupBadgePart> Parts, List<SeedGroupColor> Colors);

    /// <summary>seed/group_badge_parts.json — refreshed, since the ids have to be the imager's.</summary>
    private static async Task SeedGroupBadgePartsAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        const string file = "group_badge_parts.json";
        var content = await seedFiles.ReadJsonAsync<SeedGroupBadgeParts>(file, ct);
        if (content is null)
            return;

        var parts = await db.GroupBadgeParts.ToDictionaryAsync(p => (p.Kind, p.Id), ct);
        foreach (var seed in content.Parts)
        {
            if (!parts.TryGetValue((seed.Kind, seed.Id), out var part))
            {
                part = new GroupBadgePart { Id = seed.Id, Kind = seed.Kind };
                db.GroupBadgeParts.Add(part);
            }

            part.FileName = seed.FileName;
            part.MaskFileName = seed.MaskFileName;
        }

        var colors = await db.GroupColors.ToDictionaryAsync(c => (c.Kind, c.Id), ct);
        foreach (var seed in content.Colors)
        {
            if (!colors.TryGetValue((seed.Kind, seed.Id), out var color))
            {
                color = new GroupColor { Id = seed.Id, Kind = seed.Kind };
                db.GroupColors.Add(color);
            }

            color.Hex = seed.Hex;
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded group badge parts and colours from {File}.", file);
        }
    }
}

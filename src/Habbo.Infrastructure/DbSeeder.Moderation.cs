using Habbo.Domain.Moderation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The mod tool's reference data: the sanction ladder and the ready-made messages.</summary>
public static partial class DbSeeder
{
    private sealed record SeedSanctionType(int Id, string Name, SanctionKind Kind, int LengthHours, int ProbationDays, int Step);

    private sealed record SeedModerationPresets(List<string> User, List<string> Room);

    /// <summary>seed/sanction_types.json — the ladder a default sanction climbs. Existing steps keep
    /// whatever the hotel changed them to.</summary>
    private static async Task SeedModerationAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var types = await seedFiles.ReadJsonAsync<List<SeedSanctionType>>("sanction_types.json", ct) ?? [];
        var known = await db.SanctionTypes.Select(t => t.Id).ToListAsync(ct);
        foreach (var type in types.Where(t => !known.Contains(t.Id)))
        {
            db.SanctionTypes.Add(new SanctionType
            {
                Id = type.Id, Name = type.Name, Kind = type.Kind, LengthHours = type.LengthHours,
                ProbationDays = type.ProbationDays, Step = type.Step,
            });
        }

        // presets only fill an empty table: after that they are the hotel's own
        if (!await db.ModerationPresets.AnyAsync(ct)
            && await seedFiles.ReadJsonAsync<SeedModerationPresets>("moderation_presets.json", ct) is { } presets)
        {
            var order = 0;
            foreach (var text in presets.User)
                db.ModerationPresets.Add(new ModerationPreset { Kind = ModerationPresetKind.User, Text = text, SortOrder = order++ });
            foreach (var text in presets.Room)
                db.ModerationPresets.Add(new ModerationPreset { Kind = ModerationPresetKind.Room, Text = text, SortOrder = order++ });
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded the sanction ladder and moderation presets.");
        }
    }
}

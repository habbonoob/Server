using System.Globalization;
using System.Text.Json;
using Habbo.Domain.Furniture;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>Applies seed/furni_logic.json — what each item's SWF tells the client about it —
/// onto the item definitions: the logic and visualization it runs, the states it can draw and
/// the rotations it has artwork for. furnidata says none of this, and the server needs all of
/// it: which behaviour a click triggers, which state comes next, which rotations are real.
///
/// The file is produced by tools/furni_logic_crawl.py. Colour variants ("rare_dragonlamp*4")
/// share their base class's SWF, so entries are keyed by base class name.</summary>
public static class FurniLogicImporter
{
    private const string LogicFile = "furni_logic.json";

    /// <summary>Degrees per wire direction: the SWF lists rotations in degrees, the wire in eighths.</summary>
    private const int DegreesPerDirection = 45;

    /// <summary>Colour variants are named "{base}*{n}".</summary>
    private const char VariantSeparator = '*';

    private sealed record SwfEntry(
        string? Logic,
        string? Visualization,
        List<int>? States,
        List<int>? Directions,
        Dictionary<string, List<Dictionary<string, JsonElement>>>? LogicData);

    /// <summary>The element of a credit furni's logic XML that says what it is worth:
    /// &lt;credits value="1000"/&gt;.</summary>
    private const string CreditsElement = "credits";
    private const string ValueAttribute = "value";

    public static async Task ImportAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var entries = await seedFiles.ReadJsonAsync<Dictionary<string, SwfEntry>>(LogicFile, ct);
        if (entries is null)
        {
            logger.LogWarning(
                "{File} not found under '{Root}'; furniture keeps its basic logic and any rotation is allowed.",
                LogicFile, seedFiles.Root);
            return;
        }

        var definitions = await db.ItemDefinitions
            .Include(d => d.States)
            .Include(d => d.Directions)
            .AsSplitQuery()
            .ToListAsync(ct);

        var updated = 0;
        var missing = 0;
        foreach (var definition in definitions)
        {
            if (!entries.TryGetValue(BaseClass(definition.ClassName), out var entry))
            {
                missing++;
                continue;
            }

            if (Apply(entry, definition))
                updated++;
        }

        if (missing > 0)
            logger.LogInformation("{Missing} furniture definition(s) have no entry in {File}.", missing, LogicFile);

        if (updated == 0)
            return;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Refreshed SWF logic for {Updated} furniture definition(s) from {File}.",
            updated, LogicFile);
    }

    private static string BaseClass(string className)
    {
        var separator = className.IndexOf(VariantSeparator);
        return separator < 0 ? className : className[..separator];
    }

    private static bool Apply(SwfEntry entry, ItemDefinition definition)
    {
        var logic = string.IsNullOrEmpty(entry.Logic) ? ItemDefinition.BasicLogic : entry.Logic;
        var visualization = entry.Visualization ?? string.Empty;

        // State 0 is the resting look. The SWF only animates the states that move, so 0 is
        // often missing from its list even though every item can be in it.
        var states = (entry.States ?? []).Append(0).Distinct().Order().ToList();
        var directions = (entry.Directions ?? [])
            .Select(degrees => degrees / DegreesPerDirection)
            .Distinct().Order().ToList();

        var credits = CreditValue(entry);

        var changed = false;
        if (definition.CreditValue != credits)
        {
            definition.CreditValue = credits;
            changed = true;
        }

        if (definition.Logic != logic || definition.Visualization != visualization)
        {
            definition.Logic = logic;
            definition.Visualization = visualization;
            changed = true;
        }

        // Diffed rather than replaced: re-adding a row under the key of one just removed trips
        // the change tracker, and an unchanged definition should write nothing.
        changed |= Sync(definition.States, states, s => s.State, state => new ItemDefinitionState { State = state });
        changed |= Sync(definition.Directions, directions, d => d.Direction, d => new ItemDefinitionDirection { Direction = d });

        return changed;
    }

    /// <summary>What the item is worth in credits, or null when its SWF does not say.</summary>
    private static int? CreditValue(SwfEntry entry)
    {
        var credits = entry.LogicData?.GetValueOrDefault(CreditsElement)?.FirstOrDefault();
        if (credits is null || !credits.TryGetValue(ValueAttribute, out var value))
            return null;

        return int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    /// <summary>Makes <paramref name="rows"/> hold exactly <paramref name="wanted"/>, reporting whether it had to.</summary>
    private static bool Sync<T>(List<T> rows, IReadOnlyCollection<int> wanted, Func<T, int> value, Func<int, T> create)
    {
        var removed = rows.RemoveAll(row => !wanted.Contains(value(row)));
        var present = rows.Select(value).ToHashSet();
        var added = wanted.Where(v => !present.Contains(v)).Select(create).ToList();
        rows.AddRange(added);
        return removed > 0 || added.Count > 0;
    }
}

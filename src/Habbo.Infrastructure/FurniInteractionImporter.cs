using System.Globalization;
using Habbo.Domain.Furniture;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>Applies seed/furni_interactions.json: the server behaviours that an item's client
/// logic does not imply. The client runs a wired box as plain furniture_multistate, so without
/// this the server would toggle it like a lamp. A name with no behaviour behind it yet leaves the
/// item inert, which is what a wired box should be until the wired engine exists.
///
/// The same goes for teleports, vending machines and rollers: plain multistate or basic furniture
/// to the client, told apart only by furnidata's category.
///
/// "classes" names exact class names, "prefixes" families such as "wf_trg_", "categories"
/// a furnidata category together with the client logic it applies to: the teleport category also
/// holds a Banzai random teleporter and a step-on tent, which have logic of their own and are not
/// teleports in the paired sense. "visualizations" names how the client draws the item, for the
/// behaviours that follow from the drawing rather than the logic — a water area is multistate to
/// the client, but its state is the room's border mask and not a look to click through.
/// An exact class wins over a prefix, a longer prefix over a shorter one, a prefix over a
/// category, and a category over a visualization. Only
/// <see cref="Domain.ItemDefinition.Interaction"/> is set, so the logic read from the SWF stays intact.
///
/// seed/vending.json names the dispensers (tools/vending_from_wiki.py writes it): furni that hand out hand items or
/// effects, whatever their category, since many are "other" to furnidata. It comes after the exact classes and
/// before the rest, and sets <see cref="Domain.ItemDefinition.VendingIds"/> too; a vending machine it doesn't name
/// keeps what furnidata's custom params list.</summary>
public static class FurniInteractionImporter
{
    private const string InteractionFile = "furni_interactions.json";
    private const string VendingFile = "vending.json";

    private const string VendingMachine = "vending_machine";
    private const string EffectDispenser = "effect_vending_machine";

    private sealed record InteractionFileContent(
        Dictionary<string, string>? Prefixes,
        Dictionary<string, string>? Classes,
        Dictionary<string, Dictionary<string, string>>? Categories,
        Dictionary<string, string>? Visualizations);

    /// <summary>Class names and what each hands out.</summary>
    private sealed record VendingFileContent(
        Dictionary<string, List<int>>? HandItems,
        Dictionary<string, List<int>>? Effects);

    /// <summary>What vending.json makes of a furni: its behaviour and what it hands out.</summary>
    private sealed record Dispenser(string Interaction, List<int> Ids);

    public static async Task ImportAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var content = await seedFiles.ReadJsonAsync<InteractionFileContent>(InteractionFile, ct);
        if (content is null)
            return;
        var vending = await seedFiles.ReadJsonAsync<VendingFileContent>(VendingFile, ct);
        var handItems = vending?.HandItems ?? [];
        var effects = vending?.Effects ?? [];

        var classes = content.Classes ?? [];
        var categories = content.Categories ?? [];
        var visualizations = content.Visualizations ?? [];
        var prefixes = (content.Prefixes ?? [])
            .OrderByDescending(prefix => prefix.Key.Length)
            .ToList();

        var updated = 0;
        foreach (var definition in await db.ItemDefinitions.ToListAsync(ct))
        {
            var dispenser = handItems.TryGetValue(definition.ClassName, out var items) ? new Dispenser(VendingMachine, items)
                : effects.TryGetValue(definition.ClassName, out var worn) ? new Dispenser(EffectDispenser, worn)
                : null;
            var interaction = classes.GetValueOrDefault(definition.ClassName)
                              ?? dispenser?.Interaction
                              ?? prefixes.FirstOrDefault(p => definition.ClassName.StartsWith(p.Key, StringComparison.Ordinal)).Value
                              ?? categories.GetValueOrDefault(definition.CategoryName)?.GetValueOrDefault(definition.Logic)
                              ?? visualizations.GetValueOrDefault(definition.Visualization);
            var vendingIds = dispenser is not null && interaction == dispenser.Interaction ? dispenser.Ids
                : interaction == VendingMachine ? FromCustomParams(definition.CustomParams)
                : [];
            if (definition.Interaction == interaction && definition.VendingIds.SequenceEqual(vendingIds))
                continue;

            definition.Interaction = interaction;
            definition.VendingIds = vendingIds;
            updated++;
        }

        if (updated == 0)
            return;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Set the server behaviour of {Updated} furniture definition(s) from {File} and {Vending}.",
            updated, InteractionFile, VendingFile);
    }

    /// <summary>The hand items an old furnidata lists in a vending machine's custom params: "2,5,7".</summary>
    private static List<int> FromCustomParams(string customParams) =>
        customParams.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();
}

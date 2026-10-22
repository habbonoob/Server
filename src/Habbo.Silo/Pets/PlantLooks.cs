using Habbo.Domain.Pets;
using Habbo.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Pets;

/// <summary>Makes monster plants' looks out of their traits (seed/monster_plants.json): new ones drawn by
/// the traits' weights, bred ones from their parents' with a chance of a trait mutating. A look's rarity
/// is the sum of its traits'.</summary>
public sealed class PlantLooks(IDbContextFactory<AppDbContext> dbFactory, IOptions<PlantOptions> options)
{
    public const string PaletteTag = "palette";

    /// <summary>The layer a plant's shape is: its body.</summary>
    public const string BodyTag = "body";

    private const int FieldsPerPart = 3;
    private const int WholeChance = 100;
    private const int NoPart = -1;

    private IReadOnlyList<PlantTrait>? _traits;

    private async Task<Dictionary<string, List<PlantTrait>>> TraitsByTagAsync()
    {
        if (_traits is null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            _traits = await db.PlantTraits.AsNoTracking().ToListAsync();
        }

        return _traits.GroupBy(t => t.Tag).ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>A new plant's look, at least as rare as asked: the traits that carry rarity are drawn
    /// together from the combinations rare enough, the rest one by one.</summary>
    public async Task<PlantLook?> NewAsync(PetType type, int minimumRarity, Random random)
    {
        var byTag = await TraitsByTagAsync();
        if (!byTag.ContainsKey(PaletteTag))
            return null;

        var rare = byTag.Where(g => g.Value.Any(t => t.Rarity > 0)).Select(g => g.Key).ToList();
        var combinations = new List<List<PlantTrait>> { new() };
        foreach (var tag in rare)
            combinations = combinations.SelectMany(combination => byTag[tag].Select(trait => combination.Append(trait).ToList())).ToList();

        var fitting = combinations.Where(c => c.Sum(t => t.Rarity) >= minimumRarity).ToList();
        if (fitting.Count == 0)
            fitting = [combinations.MaxBy(c => c.Sum(t => t.Rarity))!];

        var chosen = Draw(fitting, c => c.Aggregate(1L, (weight, trait) => weight * Math.Max(1, trait.Weight)), random)
            .ToDictionary(t => t.Tag);
        foreach (var (tag, traits) in byTag.Where(g => !rare.Contains(g.Key)))
            chosen[tag] = Draw(traits, t => Math.Max(1, t.Weight), random);

        return Look(type, chosen, mutated: false);
    }

    /// <summary>A seed's look from two parents: each trait from one or the other, or now and then a new one.</summary>
    public async Task<PlantLook?> BredAsync(PetType type, Pet one, Pet two, Random random)
    {
        var byTag = await TraitsByTagAsync();
        var chosen = new Dictionary<string, PlantTrait>();
        var mutated = false;
        foreach (var (tag, traits) in byTag)
        {
            if (random.Next(WholeChance) < options.Value.MutationChance)
            {
                chosen[tag] = Draw(traits, t => Math.Max(1, t.Weight), random);
                mutated = true;
                continue;
            }

            var parent = random.Next(2) == 0 ? one : two;
            var value = tag == PaletteTag ? parent.PaletteId : PartOn(type, parent.CustomParts, tag);
            chosen[tag] = traits.FirstOrDefault(t => t.Value == value) ?? Draw(traits, t => Math.Max(1, t.Weight), random);
        }

        return chosen.ContainsKey(PaletteTag) ? Look(type, chosen, mutated) : null;
    }

    private static PlantLook Look(PetType type, Dictionary<string, PlantTrait> chosen, bool mutated)
    {
        var palette = chosen[PaletteTag].Value;
        var parts = new List<int>();
        foreach (var layer in type.Layers.OrderBy(l => l.LayerId))
        {
            if (chosen.TryGetValue(layer.Tag, out var part))
                parts.AddRange([layer.LayerId, part.Value, palette]);
        }

        return new PlantLook(palette, parts, chosen.Values.Sum(t => t.Rarity), mutated);
    }

    /// <summary>The part a plant wears on the layer of the tag; -1 for none.</summary>
    public static int PartOn(PetType type, List<int> customParts, string tag)
    {
        if (type.Layers.FirstOrDefault(l => l.Tag == tag) is not { } layer)
            return NoPart;

        for (var i = 0; i + FieldsPerPart - 1 < customParts.Count; i += FieldsPerPart)
        {
            if (customParts[i] == layer.LayerId)
                return customParts[i + 1];
        }

        return NoPart;
    }

    private static T Draw<T>(IReadOnlyList<T> items, Func<T, long> weight, Random random)
    {
        var roll = random.NextInt64(Math.Max(1, items.Sum(weight)));
        foreach (var item in items)
        {
            roll -= weight(item);
            if (roll < 0)
                return item;
        }

        return items[^1];
    }
}

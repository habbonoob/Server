using System.Text.RegularExpressions;
using Habbo.Domain.Catalog;
using Habbo.Domain.Pets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The kinds of pet (seed/pet_types.json, read from the pets' assets), the commands they learn
/// (seed/pet_commands.json), and the shop's pet offers.</summary>
public static partial class DbSeeder
{
    private sealed record SeedPetTypes(List<SeedPetType>? Types);

    private sealed record SeedPetType(
        int Id, string Code, List<SeedPetPalette>? Palettes, List<SeedPetLayer>? Layers, List<string>? Postures, List<string>? Gestures);

    private sealed record SeedPetPalette(int Id, bool Sellable, bool Rare, int Breed, int ColorTag, List<string>? Tags, bool Master);

    private sealed record SeedPetLayer(int Id, string Tag);

    private sealed record SeedPetPackages(List<SeedPetPackage>? Packages);

    private sealed record SeedPetBreeding(List<SeedPetBaby>? Babies);

    private sealed record SeedPlants(List<SeedPlantTrait>? Traits);

    private sealed record SeedPlantTrait(string Tag, int Value, int Rarity, int Weight);

    private sealed record SeedPetBaby(int Type, List<SeedRarityCategory>? Categories);

    private sealed record SeedRarityCategory(double Chance, double ChancePerLevel, List<int>? Palettes);

    private sealed record SeedPetPackage(string ClassName, int Type, List<int>? Palettes);

    private sealed record SeedPetCommands(List<SeedPetCommand>? Commands);

    private sealed record SeedPetCommand(int Id, PetCommandAction Action, int Level, int Experience, int Energy, string? Posture, int Seconds);

    /// <summary>The client reads a pet offer's kind off the end of its product code, as in "a0 pet15".</summary>
    private static readonly Regex PetOfferCode = new(@"^a0 pet(\d+)$", RegexOptions.IgnoreCase);

    /// <summary>The kinds follow their assets: what the file says of a kind replaces what the tables held.
    /// A command already in the tables keeps whatever it was changed to; a pet offer that sells no pet yet
    /// gets its pet, in place of whatever the live catalog's dump gave it.</summary>
    private static async Task SeedPetsAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var types = await seedFiles.ReadJsonAsync<SeedPetTypes>("pet_types.json", ct);
        var knownTypes = await db.PetTypes.Include(t => t.Palettes).Include(t => t.Layers).ToDictionaryAsync(t => t.Id, ct);
        foreach (var seed in types?.Types ?? [])
        {
            if (!knownTypes.TryGetValue(seed.Id, out var type))
                db.PetTypes.Add(type = new PetType { Id = seed.Id });

            type.Code = seed.Code;
            type.Postures = seed.Postures ?? [];
            type.Gestures = seed.Gestures ?? [];
            var palettes = seed.Palettes ?? [];
            type.Palettes.RemoveAll(p => palettes.All(s => s.Id != p.PaletteId));
            foreach (var source in palettes)
            {
                var palette = type.Palettes.FirstOrDefault(p => p.PaletteId == source.Id);
                if (palette is null)
                    type.Palettes.Add(palette = new PetPalette { TypeId = seed.Id, PaletteId = source.Id });
                (palette.Sellable, palette.Rare, palette.Breed, palette.ColorTag, palette.Tags, palette.Master) =
                    (source.Sellable, source.Rare, source.Breed, source.ColorTag, source.Tags ?? [], source.Master);
            }

            var layers = seed.Layers ?? [];
            type.Layers.RemoveAll(l => layers.All(s => s.Id != l.LayerId));
            foreach (var source in layers)
            {
                var layer = type.Layers.FirstOrDefault(l => l.LayerId == source.Id);
                if (layer is null)
                    type.Layers.Add(layer = new PetLayer { TypeId = seed.Id, LayerId = source.Id });
                layer.Tag = source.Tag;
            }
        }

        var commands = await seedFiles.ReadJsonAsync<SeedPetCommands>("pet_commands.json", ct);
        var knownCommands = await db.PetCommands.Select(c => c.Id).ToListAsync(ct);
        foreach (var seed in (commands?.Commands ?? []).Where(c => !knownCommands.Contains(c.Id)))
        {
            db.PetCommands.Add(new PetCommand
            {
                Id = seed.Id, Action = seed.Action, RequiredLevel = seed.Level, Experience = seed.Experience, EnergyCost = seed.Energy,
                Posture = seed.Posture, PostureSeconds = seed.Seconds,
            });
        }

        await db.SaveChangesAsync(ct);

        var typeIds = await db.PetTypes.Select(t => t.Id).ToListAsync(ct);

        // seed/pet_breeding.json: how rare a baby bred in a nest turns out; a kind the hotel already has keeps its changes
        var breeding = await seedFiles.ReadJsonAsync<SeedPetBreeding>("pet_breeding.json", ct);
        var bredKinds = await db.PetRarityCategories.Select(c => c.BabyTypeId).Distinct().ToListAsync(ct);
        foreach (var baby in (breeding?.Babies ?? []).Where(b => typeIds.Contains(b.Type) && !bredKinds.Contains(b.Type)))
        {
            var rank = 0;
            foreach (var category in baby.Categories ?? [])
            {
                db.PetRarityCategories.Add(new PetRarityCategory
                {
                    BabyTypeId = baby.Type, Rank = rank++, Chance = category.Chance, ChancePerLevel = category.ChancePerLevel,
                    Palettes = category.Palettes ?? [],
                });
            }
        }

        // seed/monster_plants.json: what monster plants look like, and how rare; a trait the hotel already has keeps its changes
        var plants = await seedFiles.ReadJsonAsync<SeedPlants>("monster_plants.json", ct);
        var knownTraits = await db.PlantTraits.Select(t => t.Tag + "/" + t.Value).ToListAsync(ct);
        foreach (var trait in (plants?.Traits ?? []).Where(t => !knownTraits.Contains(t.Tag + "/" + t.Value)))
            db.PlantTraits.Add(new PlantTrait { Tag = trait.Tag, Value = trait.Value, Rarity = trait.Rarity, Weight = trait.Weight });

        // seed/pet_packages.json: boxes a pet comes out of; one the hotel already has keeps its changes
        var packages = await seedFiles.ReadJsonAsync<SeedPetPackages>("pet_packages.json", ct);
        var knownPackages = await db.PetPackages.Select(p => p.DefinitionId).ToListAsync(ct);
        var definitions = await db.ItemDefinitions.ToDictionaryAsync(d => d.ClassName, d => d.Id, ct);
        foreach (var seed in packages?.Packages ?? [])
        {
            if (!definitions.TryGetValue(seed.ClassName, out var definitionId) || !typeIds.Contains(seed.Type))
            {
                logger.LogWarning("pet_packages.json names {Class} holding pet kind {Type}, which is not in the tables.", seed.ClassName, seed.Type);
                continue;
            }

            if (!knownPackages.Contains(definitionId))
                db.PetPackages.Add(new PetPackage { DefinitionId = definitionId, TypeId = seed.Type, Palettes = seed.Palettes ?? [] });
        }

        await db.SaveChangesAsync(ct);
        var offers = await db.CatalogOffers.Include(o => o.Products).ToListAsync(ct);
        var fixedOffers = 0;
        foreach (var offer in offers)
        {
            if (PetOfferCode.Match(offer.LocalizationId) is not { Success: true } match || !typeIds.Contains(int.Parse(match.Groups[1].Value))
                || offer.Products.Any(p => p.Type == CatalogProductType.Pet))
                continue;

            offer.Products.Clear();
            offer.Products.Add(new CatalogProduct { Type = CatalogProductType.Pet, ClassId = int.Parse(match.Groups[1].Value) });
            offer.IsPet = true;
            offer.MaxAmount = 1;
            fixedOffers++;
        }

        await db.SaveChangesAsync(ct);
        if (fixedOffers > 0)
            logger.LogInformation("Seeded {Count} pet offer(s).", fixedOffers);
    }
}

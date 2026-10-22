using Habbo.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Silo.Games.SnowWar.Arenas;

/// <summary>The arenas the hotel plays on, read from the seed once and given their furni's sizes from furnidata.</summary>
public sealed class SnowWarArenaCatalog(SeedFiles seedFiles, IDbContextFactory<AppDbContext> dbFactory, ILogger<SnowWarArenaCatalog> logger)
{
    private const string SeedFile = "snowwar_arenas.json";

    /// <summary>Game units in one unit of furni height: half a tile, as the client places furni at altitude over it.</summary>
    private const int HeightUnit = 1600;

    /// <summary>How the seed writes the rows of the floor, and how the client wants them.</summary>
    private const string RowSeparator = "\r";

    private IReadOnlyList<SnowWarArenaLevel>? _arenas;
    private readonly SemaphoreSlim _loading = new(1, 1);

    public async Task<IReadOnlyList<SnowWarArenaLevel>> AllAsync()
    {
        if (_arenas is not null)
            return _arenas;

        await _loading.WaitAsync();
        try
        {
            return _arenas ??= await LoadAsync();
        }
        finally
        {
            _loading.Release();
        }
    }

    public async Task<SnowWarArenaLevel?> ByFieldTypeAsync(int fieldType) =>
        (await AllAsync()).FirstOrDefault(a => a.FieldType == fieldType);

    private async Task<IReadOnlyList<SnowWarArenaLevel>> LoadAsync()
    {
        var file = await seedFiles.ReadJsonAsync<SnowWarArenaFile>(SeedFile);
        if (file is null)
            return [];

        await using var db = await dbFactory.CreateDbContextAsync();
        var classes = file.Arenas.SelectMany(a => a.Furni).Select(f => f.ClassName).Distinct().ToList();
        var definitions = await db.ItemDefinitions.AsNoTracking().Where(d => classes.Contains(d.ClassName))
            .ToDictionaryAsync(d => d.ClassName);

        var arenas = new List<SnowWarArenaLevel>();
        foreach (var arena in file.Arenas)
        {
            var furni = new List<SnowWarLevelFurni>();
            foreach (var seed in arena.Furni)
            {
                if (!definitions.TryGetValue(seed.ClassName, out var definition))
                {
                    logger.LogWarning("SnowStorm arena {FieldType} names furni {Class} that furnidata does not have", arena.FieldType, seed.ClassName);
                    continue;
                }

                furni.Add(new SnowWarLevelFurni(furni.Count + 1, seed.ClassName, seed.X, seed.Y, definition.XDim, definition.YDim,
                    (int)Math.Round(definition.StackHeight * HeightUnit), seed.Direction, 0, definition.CanWalk, seed.GameObject));
            }

            var spawns = arena.Spawns.ToDictionary(
                s => int.Parse(s.Key, System.Globalization.CultureInfo.InvariantCulture),
                s => (IReadOnlyList<(int X, int Y)>)s.Value.Select(p => (p[0], p[1])).ToList());
            arenas.Add(new SnowWarArenaLevel(arena.FieldType, arena.Width, arena.Height, string.Join(RowSeparator, arena.HeightMap),
                furni, spawns));
        }

        return arenas;
    }
}

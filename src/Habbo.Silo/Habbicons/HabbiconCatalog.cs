using Habbo.Infrastructure;

namespace Habbo.Silo.Habbicons;

/// <summary>The Habbicons Collection, read from seed/habbicons.json once: Habbo's own sets, as habbo.com sent them.</summary>
public sealed class HabbiconCatalog(SeedFiles seedFiles)
{
    private const string SeedFile = "habbicons.json";

    private IReadOnlyList<HabbiconCollection>? _collections;
    private readonly SemaphoreSlim _loading = new(1, 1);

    public async Task<IReadOnlyList<HabbiconCollection>> AllAsync()
    {
        if (_collections is not null)
            return _collections;

        await _loading.WaitAsync();
        try
        {
            return _collections ??= ((await seedFiles.ReadJsonAsync<HabbiconFile>(SeedFile))?.Collections ?? [])
                .Select(c => c with { Habbicons = c.Habbicons ?? [] })
                .ToList();
        }
        finally
        {
            _loading.Release();
        }
    }

    public async Task<HabbiconCollection?> FindCollectionAsync(int collectionId) =>
        (await AllAsync()).FirstOrDefault(c => c.Id == collectionId);

    /// <summary>The Habbicon, a collection's own or its reward, with the collection it belongs to.</summary>
    public async Task<(HabbiconCollection Collection, HabbiconEntry Habbicon)?> FindAsync(int habbiconId) =>
        HabbiconRules.Find(await AllAsync(), habbiconId);

    private sealed record HabbiconFile(List<HabbiconCollection>? Collections);
}

/// <summary>A collection of Habbicons: its asset name, the price of the whole set (none when it is not sold as one),
/// the Habbicon owning every one of them earns (none for a set without one), and its Habbicons in the hub's order.</summary>
public sealed record HabbiconCollection(
    int Id,
    string Name,
    int PriceCredits,
    int PriceActivityPoints,
    int ActivityPointType,
    HabbiconEntry? Reward,
    List<HabbiconEntry> Habbicons)
{
    public bool IsSold => PriceCredits > 0 || PriceActivityPoints > 0;
}

/// <summary>A Habbicon: its id and asset name, and its price, credits and activity points together (none when it is
/// not sold, only given: a collection's reward, a reward track's prize).</summary>
public sealed record HabbiconEntry(int Id, string Name, int PriceCredits, int PriceActivityPoints, int ActivityPointType)
{
    public bool IsSold => PriceCredits > 0 || PriceActivityPoints > 0;
}

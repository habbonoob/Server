using Habbo.Infrastructure;

namespace Habbo.Silo.Items;

/// <summary>What each crackable gives when it cracks open, read from seed/crackables.json once.</summary>
public sealed class CrackableCatalog(SeedFiles seedFiles)
{
    private const string SeedFile = "crackables.json";

    private IReadOnlyDictionary<string, IReadOnlyList<string>>? _prizes;
    private readonly SemaphoreSlim _loading = new(1, 1);

    /// <summary>The class names of the prizes a crackable may give; none for one the seed does not name.</summary>
    public async Task<IReadOnlyList<string>> PrizesOfAsync(string className)
    {
        if (_prizes is null)
        {
            await _loading.WaitAsync();
            try
            {
                _prizes ??= ((await seedFiles.ReadJsonAsync<CrackableFile>(SeedFile))?.Crackables ?? [])
                    .ToDictionary(c => c.ClassName, c => (IReadOnlyList<string>)c.Prizes, StringComparer.Ordinal);
            }
            finally
            {
                _loading.Release();
            }
        }

        return _prizes.GetValueOrDefault(className) ?? [];
    }

    private sealed record CrackableFile(List<CrackableSeed> Crackables);

    private sealed record CrackableSeed(string ClassName, List<string> Prizes);
}

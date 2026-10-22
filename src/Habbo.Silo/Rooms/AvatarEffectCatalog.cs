using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Rooms;

/// <summary>Habbo's avatar effects, read from the database once and kept in memory.</summary>
public sealed class AvatarEffectCatalog(IDbContextFactory<AppDbContext> dbFactory) : IAvatarEffectCatalog, Habbo.Silo.Abstractions.Hotel.IReloadable
{
    public string ReloadName => "effects";

    public Task ReloadAsync()
    {
        _byLibrary = null;
        _ids = null;
        return Task.CompletedTask;
    }

    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private Dictionary<string, int>? _byLibrary;
    private HashSet<int>? _ids;

    public async Task<int?> FindAsync(string library, CancellationToken ct = default)
    {
        await LoadAsync(ct);
        return _byLibrary!.TryGetValue(library, out var id) ? id : null;
    }

    public async Task<bool> ExistsAsync(int effectId, CancellationToken ct = default)
    {
        await LoadAsync(ct);
        return _ids!.Contains(effectId);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        if (_ids is not null)
            return;

        await _loadLock.WaitAsync(ct);
        try
        {
            if (_ids is not null)
                return;

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var effects = await db.AvatarEffects.AsNoTracking().ToListAsync(ct);
            _byLibrary = effects.GroupBy(e => e.Library, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
            _ids = effects.Select(e => e.Id).ToHashSet();
        }
        finally
        {
            _loadLock.Release();
        }
    }
}

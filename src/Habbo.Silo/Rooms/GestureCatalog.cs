using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Chat;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Rooms;

/// <summary>The emoticon-to-gesture map, read from the database once and kept in memory —
/// chat runs through this on every message and the table is tiny and rarely edited.</summary>
public sealed class GestureCatalog(IDbContextFactory<AppDbContext> dbFactory) : IGestureCatalog, Habbo.Silo.Abstractions.Hotel.IReloadable
{
    public string ReloadName => "gestures";

    public Task ReloadAsync()
    {
        _map = null;
        return Task.CompletedTask;
    }

    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private IReadOnlyList<(string Emoticon, Gesture Gesture)>? _map;

    public async Task<Gesture> DetectAsync(string text, CancellationToken ct = default) =>
        GestureMatcher.Detect(text, await GetMapAsync(ct));

    private async Task<IReadOnlyList<(string Emoticon, Gesture Gesture)>> GetMapAsync(CancellationToken ct)
    {
        if (_map is not null)
            return _map;

        await _loadLock.WaitAsync(ct);
        try
        {
            if (_map is not null)
                return _map;

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var rows = await db.ChatGestures.AsNoTracking().ToListAsync(ct);
            _map = rows.Select(row => (row.Emoticon, (Gesture)row.GestureId)).ToList();
            return _map;
        }
        finally
        {
            _loadLock.Release();
        }
    }
}

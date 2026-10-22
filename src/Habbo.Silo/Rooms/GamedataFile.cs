using Habbo.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Rooms;

/// <summary>A key=value gamedata file the hotel serves the client, read once from web_gamedata.</summary>
public abstract class GamedataFile(IDbContextFactory<AppDbContext> dbFactory, string name) : Habbo.Silo.Abstractions.Hotel.IReloadable
{
    /// <summary>Read again on next use: after the file was changed.</summary>
    public Task ReloadAsync()
    {
        _values = null;
        return Task.CompletedTask;
    }

    public abstract string ReloadName { get; }

    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private Dictionary<string, string>? _values;

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        if (_values is null)
        {
            await _loadLock.WaitAsync(ct);
            try
            {
                if (_values is null)
                {
                    await using var db = await dbFactory.CreateDbContextAsync(ct);
                    var content = await db.WebGamedata.AsNoTracking().Where(g => g.Name == name)
                        .Select(g => g.Content).FirstOrDefaultAsync(ct) ?? string.Empty;
                    var values = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var line in content.Split('\n'))
                    {
                        var equals = line.IndexOf('=');
                        if (equals > 0)
                            values[line[..equals]] = line[(equals + 1)..].TrimEnd('\r');
                    }

                    _values = values;
                }
            }
            finally
            {
                _loadLock.Release();
            }
        }

        return _values.GetValueOrDefault(key);
    }
}

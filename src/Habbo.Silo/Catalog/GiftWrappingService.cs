using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Catalog;

/// <summary>The gift wraps: the configured class names looked up once in the item definitions.</summary>
public sealed class GiftWrappingService(IDbContextFactory<AppDbContext> dbFactory, IOptions<GiftWrappingOptions> options)
    : IGiftWrappingService, Habbo.Silo.Abstractions.Hotel.IReloadable
{
    public string ReloadName => "giftwrapping";

    public Task ReloadAsync()
    {
        _catalog = null;
        return Task.CompletedTask;
    }

    private GiftWrappingCatalog? _catalog;

    public async Task<GiftWrappingCatalog> GetAsync(CancellationToken ct = default)
    {
        if (_catalog is not null)
            return _catalog;

        var wrapping = options.Value;
        var names = wrapping.WrapClassNames.Concat(wrapping.DefaultClassNames).ToList();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var ids = await db.ItemDefinitions.AsNoTracking().Where(d => names.Contains(d.ClassName))
            .ToDictionaryAsync(d => d.ClassName, d => d.ClassId, ct);

        List<int> Resolve(IEnumerable<string> classNames) => classNames.Where(ids.ContainsKey).Select(n => ids[n]).ToList();
        return _catalog = new GiftWrappingCatalog(wrapping.Enabled, wrapping.Price,
            Resolve(wrapping.WrapClassNames), wrapping.BoxTypes, wrapping.RibbonTypes, Resolve(wrapping.DefaultClassNames));
    }
}

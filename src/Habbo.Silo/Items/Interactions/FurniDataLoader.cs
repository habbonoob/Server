using Habbo.Domain.Furniture;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>Loads items' data: the furni_* layout tables first, then whatever behaviours that
/// own their items' data build from their own tables. Items must come with their definition.</summary>
public sealed class FurniDataLoader(FurniInteractionRegistry interactions)
{
    public async Task<Dictionary<int, FurniDataView>> LoadAsync(
        AppDbContext db, IReadOnlyCollection<UserItem> items, CancellationToken ct = default)
    {
        var views = await FurniDataStore.LoadAsync(db, items, ct);

        foreach (var group in items.GroupBy(item => interactions.For(item.Definition!)))
        {
            if (group.Key is IFurniDataOwner owner)
                await owner.LoadAsync(db, group.ToList(), views, ct);
        }

        return views;
    }

    public async Task<FurniDataView> LoadAsync(AppDbContext db, UserItem item, CancellationToken ct = default) =>
        (await LoadAsync(db, [item], ct))[item.Id];
}

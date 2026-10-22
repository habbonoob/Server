using Habbo.Domain.Furniture;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A behaviour that keeps its items' data in tables of its own, because what it
/// stores has a meaning of its own — a trophy's engraving is a name, a date and a message, not
/// the tab-separated string the client happens to want.</summary>
public interface IFurniDataOwner
{
    /// <summary>Replaces the entries in <paramref name="views"/> for <paramref name="items"/>
    /// with the data built from the behaviour's own tables.</summary>
    Task LoadAsync(
        AppDbContext db, IReadOnlyCollection<UserItem> items, IDictionary<int, FurniDataView> views, CancellationToken ct);
}

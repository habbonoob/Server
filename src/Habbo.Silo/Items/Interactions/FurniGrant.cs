using Habbo.Domain.Furniture;
using Habbo.Infrastructure;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A newly bought item, not yet saved, with what the purchase said about it. A
/// behaviour may add further items to the same purchase — a teleport is sold as a pair — so
/// what the buyer ends up holding is <see cref="Items"/>, not just the one item.</summary>
public sealed class FurniGrant(
    AppDbContext db,
    UserItem item,
    ItemDefinition definition,
    string productParam,
    string clientParam,
    string buyerName,
    DateTime at)
{
    public AppDbContext Db { get; } = db;

    /// <summary>The item the catalog product named.</summary>
    public UserItem Item { get; } = item;

    public ItemDefinition Definition { get; } = definition;

    /// <summary>The catalog product's own parameter.</summary>
    public string ProductParam { get; } = productParam;

    /// <summary>What the buyer typed into the offer, e.g. a trophy's engraving.</summary>
    public string ClientParam { get; } = clientParam;

    public string BuyerName { get; } = buyerName;
    public DateTime At { get; } = at;

    /// <summary>Everything this product hands over, the named item first.</summary>
    public IReadOnlyList<UserItem> Items => _items;
    private readonly List<UserItem> _items = [item];

    /// <summary>Adds another item of the same kind to the purchase, for a behaviour that comes
    /// in more than one piece.</summary>
    public UserItem AddItem()
    {
        var extra = new UserItem { OwnerId = Item.OwnerId, DefinitionId = Item.DefinitionId };
        Db.UserItems.Add(extra);
        _items.Add(extra);
        return extra;
    }
}

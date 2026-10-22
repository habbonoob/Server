using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>What a kind of furniture does: the data it starts with, who may use it and what a
/// use changes. One instance serves every item of its kind across the silo, so anything that
/// belongs to a single item lives on that item, never here.</summary>
public abstract class FurniInteraction
{
    public virtual FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Nobody;

    /// <summary>Gives a newly bought item its data. By default that is the state the catalog
    /// product names, if any — a poster's number, a wallpaper's pattern.</summary>
    public virtual void Grant(FurniGrant grant)
    {
        if (!string.IsNullOrEmpty(grant.ProductParam))
            FurniDataStore.Attach(grant.Db, grant.Item, FurniDataView.WithState(grant.ProductParam));
    }

    /// <summary>The data a room shows the item with. Most furniture shows what it holds.</summary>
    public virtual FurniDataView InRoom(FurniDataView data) => data;

    /// <summary>Whether the item may be bought with what the buyer sent along, before anything is taken. Most
    /// furniture takes anything.</summary>
    public virtual Task<bool> AcceptsOrderAsync(AppDbContext db, Guid buyerId, string clientParam) => Task.FromResult(true);

    /// <summary>Answers a use the room has already allowed under <see cref="UsagePolicy"/>.
    /// Anything the behaviour does not recognise is ignored.</summary>
    public virtual Task UseAsync(FurniUse use) => Task.CompletedTask;

    /// <summary>An avatar stepped onto the item. Most furniture takes no notice.</summary>
    public virtual Task WalkedOnAsync(FurniWalk walk) => Task.CompletedTask;
}

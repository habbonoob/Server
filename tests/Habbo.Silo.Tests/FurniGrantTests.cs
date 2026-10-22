using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Items;
using Habbo.Silo.Items.Interactions;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Tests;

/// <summary>What a purchase leaves behind, tracked but never saved: the context is built against
/// the real provider so the model is the real one, and nothing here opens a connection.</summary>
public class FurniGrantTests
{
    private static readonly FurniInteractionRegistry Interactions = new();

    private static AppDbContext Context() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=none;Database=none").Options);

    private static ItemDefinition Definition(string interaction) => new()
    {
        Id = 4, ClassName = "door", Logic = "furniture_multistate", Interaction = interaction,
    };

    private static FurniGrant Grant(AppDbContext db, ItemDefinition definition, string clientParam = "")
    {
        var item = new UserItem { OwnerId = Guid.NewGuid(), DefinitionId = definition.Id };
        db.UserItems.Add(item);
        return new FurniGrant(db, item, definition, string.Empty, clientParam, "tester", DateTime.UtcNow);
    }

    [Fact]
    public void BuyingATeleportBuysThePair()
    {
        using var db = Context();
        var definition = Definition("teleport");
        var grant = Grant(db, definition);

        Interactions.For(definition).Grant(grant);

        Assert.Equal(2, grant.Items.Count);
        Assert.All(grant.Items, item => Assert.Equal(definition.Id, item.DefinitionId));
        Assert.Equal(grant.Items[0].OwnerId, grant.Items[1].OwnerId);

        // Both sides are stored, each pointing at the other.
        var links = db.ChangeTracker.Entries<FurniTeleportLink>().Select(e => e.Entity).ToList();
        Assert.Equal(2, links.Count);
        Assert.Contains(links, l => ReferenceEquals(l.Item, grant.Items[0]) && ReferenceEquals(l.LinkedItem, grant.Items[1]));
        Assert.Contains(links, l => ReferenceEquals(l.Item, grant.Items[1]) && ReferenceEquals(l.LinkedItem, grant.Items[0]));
    }

    /// <summary>Habbo placed a football counter showing "30": its first preset.</summary>
    [Theory]
    [InlineData("30,60,120,180,300,600", "30")]
    [InlineData("", "0")]
    public void ABoughtCounterShowsItsFirstTime(string presets, string shown)
    {
        using var db = Context();
        var definition = Definition("furniture_counter_clock");
        definition.CustomParams = presets;
        var grant = Grant(db, definition);

        Interactions.For(definition).Grant(grant);

        Assert.Equal(shown, Assert.Single(db.ChangeTracker.Entries<FurniState>()).Entity.State);
    }

    [Fact]
    public void BuyingCreditFurniLeavesItPlainSoItStacks()
    {
        using var db = Context();
        var definition = Definition("furniture_credit");
        definition.CreditValue = 1000;
        var item = new UserItem { OwnerId = Guid.NewGuid(), DefinitionId = definition.Id };
        db.UserItems.Add(item);

        // The catalog product's parameter repeats the item's worth; it is not a state.
        var grant = new FurniGrant(db, item, definition, "1000", string.Empty, "tester", DateTime.UtcNow);
        Interactions.For(definition).Grant(grant);

        Assert.Empty(db.ChangeTracker.Entries<FurniState>());
        Assert.True(InventoryProjection.From(item, definition, FurniDataView.Plain).Groupable);
    }

    [Fact]
    public void BuyingATrophyEngravesIt()
    {
        using var db = Context();
        var definition = Definition("furniture_trophy");
        var grant = Grant(db, definition, "Best builder\tof 2026");

        Interactions.For(definition).Grant(grant);

        var trophy = Assert.Single(db.ChangeTracker.Entries<FurniTrophy>().Select(e => e.Entity));
        Assert.Equal("tester", trophy.EngraverName);
        Assert.Same(grant.Item, trophy.Item);

        // A tab would split the engraving into another field in the client's trophy widget.
        Assert.Equal("Best builder of 2026", trophy.Message);
        Assert.Single(grant.Items);
    }
}

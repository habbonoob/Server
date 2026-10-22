using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;

namespace Habbo.Domain.Furniture;

/// <summary>An item a player owns — in their inventory, or placed in a room (RoomId set).</summary>
public sealed class UserItem
{
    public int Id { get; set; }
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
    public int DefinitionId { get; set; }
    public ItemDefinition? Definition { get; set; }
    /// <summary>Which layout this item's data uses. The payload itself lives in the furni_*
    /// table for that layout, so a dice's state and a scoreboard's rows are not competing for
    /// one column.</summary>
    public FurniDataType DataType { get; set; } = FurniDataType.Legacy;

    /// <summary>Set only on a limited edition: its number in the run, and how long the run was.
    /// The client reads both from a trailer it only looks for when the item is one.</summary>
    public int? UniqueSerialNumber { get; set; }
    public int? UniqueSeriesSize { get; set; }

    /// <summary>The group group furniture was bought for: its badge and colours are the group's.</summary>
    public int? GroupId { get; set; }

    // Placement state: null while the item sits in the inventory.
    public int? RoomId { get; set; }
    public Room? Room { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
    public double? Z { get; set; }
    public int? Rotation { get; set; }
    public string? WallPosition { get; set; }

    // Inventory/trade attributes — all wire-visible, all owned by the row.
    public bool Recyclable { get; set; } = true;
    public bool Tradeable { get; set; } = true;

    /// <summary>Up for sale on the marketplace: out of the owner's inventory, in escrow.</summary>
    public bool InMarketplace { get; set; }

    /// <summary>The chest the item is stored in, out of its owner's inventory; null for any other item.</summary>
    public int? ChestId { get; set; }

    /// <summary>When the item went into its chest, for oldest and most recent first, and the transaction that put it there.</summary>
    public DateTimeOffset? StoredAt { get; set; }
    public long? StoredInTransactionId { get; set; }

    /// <summary>The item's place in its chest's random order, drawn when it went in: wired hands out
    /// "random" items lowest first, so the next ones are known ahead.</summary>
    public int? ChestDrawOrder { get; set; }

    /// <summary>When the item came into being, which a trade shows.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool Sellable { get; set; } = true;
    public int SecondsToExpiration { get; set; } = -1;
    public bool HasRentPeriodStarted { get; set; }
    public string SlotId { get; set; } = string.Empty;
    public int Extra { get; set; }
    /// <summary>Seconds until a rental runs out, or <see cref="NeverExpires"/>. Habbo sends -1
    /// for ordinary furniture; 0 would read as a rental that has just run out.</summary>
    public int ExpiryTime { get; set; } = NeverExpires;

    public const int NeverExpires = -1;

    /// <summary>Borrowed from the Builders Club warehouse rather than owned: placed straight from it, counted against
    /// the owner's club limit, and gone back to the warehouse (deleted) when picked up; it never sits in an
    /// inventory, so it is never traded, sold or recycled.</summary>
    public bool BuildersClub { get; set; }
}

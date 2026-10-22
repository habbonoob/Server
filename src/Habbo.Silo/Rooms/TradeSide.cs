using Habbo.Protocol.Messages.Outgoing.Inventory.Trading;

namespace Habbo.Silo.Rooms;

/// <summary>One side of a trade: who, what they offer (as the client shows it, and what each credit
/// furni is worth), whether they may give at all, and how far they have agreed.</summary>
public sealed class TradeSide(RoomEntityStore.RoomEntity entity, bool mayGive)
{
    public RoomEntityStore.RoomEntity Entity { get; } = entity;

    /// <summary>False for an account without trading in use: it only receives.</summary>
    public bool MayGive { get; } = mayGive;

    /// <summary>The offered items by id, in the order they were put in.</summary>
    public Dictionary<int, TradeItemData> Items { get; } = [];

    /// <summary>The credits each offered credit furni is worth, by item id.</summary>
    public Dictionary<int, int> CreditValues { get; } = [];

    public bool Accepted { get; set; }

    public bool Confirmed { get; set; }

    public int Credits => CreditValues.Values.Sum();
}

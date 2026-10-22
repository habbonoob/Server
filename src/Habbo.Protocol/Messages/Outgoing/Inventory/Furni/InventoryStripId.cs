namespace Habbo.Protocol.Messages.Outgoing.Inventory.Furni;

/// <summary>How Habbo names an item in the inventory: its id negated, a "strip id", with the real id beside it as the
/// item's ref. Every inventory entry, unseen mark and removal carries the strip id, and the client sends it back for
/// what it does with the item: place, trade, sell, recycle.
///
/// The client depends on it. Placing an item it compares the placed object's id, negated, to the ref; cancelling the
/// mover it checks the ref is not negative; using a pet product from the inventory it sends the ref.</summary>
public static class InventoryStripId
{
    public static int Of(int itemId) => -itemId;

    /// <summary>The item a strip id the client sent names.</summary>
    public static int ItemOf(int stripId) => -stripId;
}

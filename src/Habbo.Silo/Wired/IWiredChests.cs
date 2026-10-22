using Habbo.Silo.Chests;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>What wired may do with the room's chests. It only touches Wired chests that are not locked.</summary>
public interface IWiredChests
{
    /// <summary>Whether wired may use the item as a chest.</summary>
    bool IsUsable(RoomItemStore.PlacedItem item);

    /// <summary>The kind a placed item is, for boxes that name kinds by example.</summary>
    ChestKind KindOf(RoomItemStore.PlacedItem item);

    /// <summary>What the chest holds: its credits, or its items — only of the kinds given, if any, and only those it
    /// shows in its preview, if asked.</summary>
    int Count(RoomItemStore.PlacedItem chest, IReadOnlySet<ChestKind>? kinds = null, bool previewedOnly = false);

    /// <summary>Gives the user credits from the chests, in the order they come, into the earnings category; all they
    /// hold when no amount is given. Nothing is given when they hold too little.</summary>
    Task GiveCoinsAsync(IReadOnlyList<RoomItemStore.PlacedItem> chests, RoomEntityStore.RoomEntity user, int? amount,
        byte earningsCategory, WiredChestGift gift);

    /// <summary>Gives the user items from the chests, picked in the iteration's order; all they hold when no amount is
    /// given. Nothing is given when they hold too few.</summary>
    Task GiveItemsAsync(IReadOnlyList<RoomItemStore.PlacedItem> chests, RoomEntityStore.RoomEntity user, int? amount,
        int iteration, WiredChestGift gift);

    /// <summary>The contract a contract furni holds; none for anything else.</summary>
    Task<Habbo.Domain.Furniture.Chests.WiredContract?> ContractOfAsync(RoomItemStore.PlacedItem item);

    /// <summary>Starts a transaction for the user under a contract: a reward at once, a payment or trade in the trade
    /// window, called off after so many seconds (0: never).</summary>
    Task StartContractAsync(RoomEntityStore.RoomEntity user, ContractTerms terms, int timeoutSeconds);

    /// <summary>Calls off the user's transaction under one of the contracts, or under any.</summary>
    Task CancelContractsAsync(RoomEntityStore.RoomEntity user, IReadOnlySet<int>? contractIds);

    /// <summary>Tells the user a wired transaction failed, and why.</summary>
    void Fail(RoomEntityStore.RoomEntity user, int failure);
}

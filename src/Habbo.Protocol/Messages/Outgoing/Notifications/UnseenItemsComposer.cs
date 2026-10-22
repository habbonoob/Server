using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Notifications;

/// <summary>2362 UnseenItems — which items the player has not looked at yet, grouped by
/// inventory category. This is what puts the count on the inventory button: the client sums the
/// ids per category and shows the total, then highlights those tiles when the inventory opens.
///
/// Without it a purchase lands silently — the item is in the inventory, but nothing says so.</summary>
[Message(ServerMessages.UnseenItemsComposer)]
public sealed partial record UnseenItemsComposer(IReadOnlyList<UnseenItemCategoryData> Categories)
{
    /// <summary>The client's own inventory categories. Only these five count toward the badge.</summary>
    public const int OwnedFurniCategory = 1;
    public const int RentedFurniCategory = 2;
    public const int PetCategory = 3;
    public const int BadgeCategory = 4;
    public const int BotCategory = 5;

    /// <summary>Furniture the player just gained, by the strip ids its inventory entries carry.</summary>
    public static UnseenItemsComposer OwnedFurni(IReadOnlyList<int> itemIds) =>
        new([new UnseenItemCategoryData(OwnedFurniCategory, itemIds.Select(InventoryStripId.Of).ToList())]);

    /// <summary>Badges the player just gained.</summary>
    public static UnseenItemsComposer Badges(IReadOnlyList<int> badgeIds) =>
        new([new UnseenItemCategoryData(BadgeCategory, badgeIds)]);

    /// <summary>Pets the player just gained.</summary>
    public static UnseenItemsComposer Pets(IReadOnlyList<int> petIds) =>
        new([new UnseenItemCategoryData(PetCategory, petIds)]);

    /// <summary>Bots the player just gained.</summary>
    public static UnseenItemsComposer Bots(IReadOnlyList<int> botIds) =>
        new([new UnseenItemCategoryData(BotCategory, botIds)]);
}

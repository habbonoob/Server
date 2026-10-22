namespace Habbo.Domain.Rooms;

/// <summary>What a user did with the navigator's promoted rooms.</summary>
public enum RoomAdInteractionKind
{
    /// <summary>Opened the events tab.</summary>
    TabViewed,

    /// <summary>Clicked a promoted room in the events tab.</summary>
    AdClicked,

    /// <summary>Came into a promoted room from a promoted category.</summary>
    PromotedEntry,

    /// <summary>Opened the catalog's room ad page, starting to buy one.</summary>
    PurchaseInitiated,
}

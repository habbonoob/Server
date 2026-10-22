namespace Habbo.Domain.Navigator;

/// <summary>A hand-picked list of rooms the official view promotes ("partyRoom", "freezeRoom"),
/// named by the code the client's texts know it by ("promotedroomcategory.partyRoom").</summary>
public sealed class PromotedRoomCategory
{
    public required string Code { get; set; }

    public int SortOrder { get; set; }

    /// <summary>The rooms, the leading one first.</summary>
    public List<int> RoomIds { get; set; } = [];
}

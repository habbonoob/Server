namespace Habbo.Domain.Navigator;

/// <summary>A category room promotions are listed under. The ids are the client's: its texts name
/// each one by id ("roomevent_type_N", "eventcategory__N").</summary>
public sealed class RoomEventCategory
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public bool Visible { get; set; } = true;

    public int SortOrder { get; set; }
}

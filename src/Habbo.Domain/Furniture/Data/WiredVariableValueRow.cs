namespace Habbo.Domain.Furniture.Data;

/// <summary>A stored value of a permanent wired variable: which room's variable (the variable
/// box's item id, as the variable's id), who holds it (a furni, a user, or the room itself when
/// neither is set), the value, and when it was given and last changed.</summary>
public sealed class WiredVariableValueRow
{
    public const int VariableIdMaxLength = 64;

    public long Id { get; set; }
    public int RoomId { get; set; }
    public required string VariableId { get; set; }
    public int? ItemId { get; set; }
    public Guid? UserId { get; set; }
    public int Value { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

namespace Habbo.Domain.Furniture;

/// <summary>One rotation an item's SWF has artwork for, as a wire direction (0–7, in 45° steps).
/// A rotation without artwork draws nothing, so the server refuses it.</summary>
public sealed class ItemDefinitionDirection
{
    public int DefinitionId { get; set; }
    public int Direction { get; set; }
}

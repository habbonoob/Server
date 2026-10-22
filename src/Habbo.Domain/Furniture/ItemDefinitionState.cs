namespace Habbo.Domain.Furniture;

/// <summary>One visual state an item's SWF can draw. Behaviours step through these: a lamp
/// goes 0 → 1 → 0, a dice shows one of 1–6 and -1 while it rolls.</summary>
public sealed class ItemDefinitionState
{
    public int DefinitionId { get; set; }
    public int State { get; set; }
}

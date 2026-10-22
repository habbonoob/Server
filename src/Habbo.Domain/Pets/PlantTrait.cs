namespace Habbo.Domain.Pets;

/// <summary>Something a monster plant's look is made of — its palette, or a part on one of its layers —
/// how rare it is, and how likely a new plant is to have it.</summary>
public sealed class PlantTrait
{
    /// <summary>What the value picks: "palette", or the tag of a layer the part goes on.</summary>
    public string Tag { get; set; } = string.Empty;

    public int Value { get; set; }

    public int Rarity { get; set; }

    public int Weight { get; set; }
}

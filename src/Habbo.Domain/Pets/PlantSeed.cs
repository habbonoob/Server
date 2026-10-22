using Habbo.Domain.Furniture;

namespace Habbo.Domain.Pets;

/// <summary>What a seed two plants gave will grow into; a seed from the shop has none and grows into
/// whatever comes up.</summary>
public sealed class PlantSeed
{
    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public int PaletteId { get; set; }

    /// <summary>Layer, part and palette in turn, as a pet's custom parts.</summary>
    public List<int> CustomParts { get; set; } = [];

    public int Rarity { get; set; }
}

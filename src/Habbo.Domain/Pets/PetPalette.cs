namespace Habbo.Domain.Pets;

/// <summary>A palette a kind of pet can be drawn in: the breed it belongs to, the colour it is tagged as,
/// the layers it colours, whether it is their default, whether the shop sells it and whether it is rare.</summary>
public sealed class PetPalette
{
    public int TypeId { get; set; }

    public int PaletteId { get; set; }

    public bool Sellable { get; set; }

    public bool Rare { get; set; }

    public int Breed { get; set; }

    /// <summary>The colour it is tagged as, by the client's colour tag number; -1 for none.</summary>
    public int ColorTag { get; set; } = -1;

    /// <summary>The layer tags it colours.</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>The palette its layers are drawn in by default.</summary>
    public bool Master { get; set; }
}

namespace Habbo.Domain.Pets;

/// <summary>A kind of pet, by the client's type id: the palettes it comes in and the postures and
/// gestures its asset can show.</summary>
public sealed class PetType
{
    public int Id { get; set; }

    /// <summary>Its asset's name, such as "dog".</summary>
    public string Code { get; set; } = string.Empty;

    public List<string> Postures { get; set; } = [];

    public List<string> Gestures { get; set; } = [];

    public List<PetPalette> Palettes { get; set; } = [];

    public List<PetLayer> Layers { get; set; } = [];
}

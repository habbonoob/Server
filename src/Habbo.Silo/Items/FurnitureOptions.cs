namespace Habbo.Silo.Items;

/// <summary>How the hotel's furniture behaves where it is not the item's own data (config section "Furniture").</summary>
public sealed class FurnitureOptions
{
    /// <summary>The colours a note may be, as the client's note widget offers them.</summary>
    public List<string> PostItColors { get; set; } = [];

    /// <summary>The colour a note goes up in.</summary>
    public string PostItDefaultColor { get; set; } = string.Empty;

    /// <summary>The longest text a note holds.</summary>
    public int PostItTextMaxLength { get; set; } = 366;

    public DimmerOptions Dimmer { get; set; } = new();

    /// <summary>The set types that are clothing: what a mannequin or a changing gate keeps of a look.</summary>
    public List<string> OutfitSetTypes { get; set; } = [];

    public int MannequinNameMaxLength { get; set; } = 32;

    /// <summary>The kinds of furniture (by logic) whose map data a room's owner may set; staff set any.</summary>
    public List<string> OwnerEditableDataLogics { get; set; } = [];

    /// <summary>The longest key or value an item's map data takes.</summary>
    public int ObjectDataValueMaxLength { get; set; } = 256;
}

namespace Habbo.Camera;

/// <summary>Where the camera's asset libraries are, read once from the mirror's folders.</summary>
internal sealed class AssetIndex
{
    /// <summary>The client's own libraries, searched first for every name.</summary>
    public List<string> Local { get; } = [];

    /// <summary>A furni's library by its name (the newest revision's).</summary>
    public Dictionary<string, string> Furni { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The avatar's, effects' and pets' libraries by name (the newest flash assets').</summary>
    public Dictionary<string, string> Gordon { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>"{part type}:{part id}" to the library figuremap.xml puts the part in.</summary>
    public Dictionary<string, string> Parts { get; } = new(StringComparer.Ordinal);

    /// <summary>A part id to the libraries figuremap.xml puts a part of that id in, of any type: a set's sleeves or
    /// hair below a hat are in the library of their sibling part of the same id, which figuremap may not list them in.</summary>
    public Dictionary<string, List<string>> PartsById { get; } = new(StringComparer.Ordinal);

    /// <summary>An effect's id to its library (effectmap.xml).</summary>
    public Dictionary<string, string> Effects { get; } = new(StringComparer.Ordinal);
}

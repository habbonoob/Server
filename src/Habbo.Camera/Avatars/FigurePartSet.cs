namespace Habbo.Camera.Avatars;

/// <summary>A figure set (figuredata's set): its parts, and the part types it hides on the avatar (a hat hiding hair).
/// Parts are kept as the client keeps them: a part goes before the first one of its type with a lower index.</summary>
public sealed class FigurePartSet
{
    private readonly List<FigurePart> _parts = [];

    public FigurePartSet(IEnumerable<FigurePart> parts, IEnumerable<string> hiddenLayers)
    {
        foreach (var part in parts)
        {
            var at = _parts.FindIndex(p => p.Type == part.Type && p.Index < part.Index);
            if (at != -1)
                _parts.Insert(at, part);
            else
                _parts.Add(part);
        }
        HiddenLayers = hiddenLayers.ToList();
    }

    public IReadOnlyList<FigurePart> Parts => _parts;

    public IReadOnlyList<string> HiddenLayers { get; }
}

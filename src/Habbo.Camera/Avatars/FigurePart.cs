namespace Habbo.Camera.Avatars;

/// <summary>One layer of a figure set (figuredata's part): its type and id, which of the set's colours it takes (0 for
/// none), and its place among the set's parts of the same type.</summary>
public sealed record FigurePart(string Type, int Id, int Index, int ColorIndex);

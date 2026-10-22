namespace Habbo.Camera.Avatars;

/// <summary>A set type (figuredata's settype: "hr", "ch"…): the palette its colours come from, and its sets by id.</summary>
public sealed record FigureSetType(string Type, int PaletteId, IReadOnlyDictionary<int, FigurePartSet> Sets);

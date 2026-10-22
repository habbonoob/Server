namespace Habbo.Silo.Wired.Addons.VariableFx;

/// <summary>A style of a variable display, as the client knows it: the colours, renderers and widths it allows and those
/// it falls back to (by the client's ids; -1 is "not applicable"), and the extras it is sent with.</summary>
public sealed record VariableFxStyle(
    int Category, int StyleId,
    IReadOnlyList<int> Colors, int DefaultColor,
    IReadOnlyList<int> Renderers, int DefaultRenderer,
    IReadOnlyList<int> Widths, int DefaultWidth,
    IReadOnlyList<(string Key, string Value)> Extras);

using System.Text.Json.Serialization;

namespace Habbo.Camera;

/// <summary>One sprite in view, in the client's drawing order (far to near): an asset by its name (or an image by
/// its url), at its place on the photo's canvas, with how the client drew it.</summary>
public sealed record RenderSprite(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("x")] double X,
    [property: JsonPropertyName("y")] double Y,
    [property: JsonPropertyName("z")] double Z,
    [property: JsonPropertyName("alpha")] int? Alpha,
    [property: JsonPropertyName("flipH")] bool FlipH,
    [property: JsonPropertyName("skew")] double? Skew,
    [property: JsonPropertyName("frame")] bool Frame,
    [property: JsonPropertyName("color")] int? Color,
    [property: JsonPropertyName("blendMode")] string? BlendMode,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("posture")] string? Posture,
    [property: JsonPropertyName("paletteSourceName")] string? PaletteSourceName);

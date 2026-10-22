using System.Text.Json.Serialization;

namespace Habbo.Camera;

/// <summary>A wall, floor or landscape surface (or the photo's background, the first): the quad its corners make
/// on the canvas, filled with its colour and its texture's columns, cut by the masks of the windows on it.</summary>
public sealed record RenderPlane(
    [property: JsonPropertyName("z")] double Z,
    [property: JsonPropertyName("cornerPoints")] IReadOnlyList<RenderPoint> CornerPoints,
    [property: JsonPropertyName("color")] uint Color,
    [property: JsonPropertyName("bottomAligned")] bool BottomAligned,
    [property: JsonPropertyName("texCols")] IReadOnlyList<RenderTextureColumn>? TexCols,
    [property: JsonPropertyName("masks")] IReadOnlyList<RenderMask>? Masks);

using System.Text.Json.Serialization;

namespace Habbo.Camera;

/// <summary>A hole a window cuts in a wall: the mask asset, where on the plane, flipped or not.</summary>
public sealed record RenderMask(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("location")] RenderPoint Location,
    [property: JsonPropertyName("flipH")] bool FlipH,
    [property: JsonPropertyName("flipV")] bool FlipV);

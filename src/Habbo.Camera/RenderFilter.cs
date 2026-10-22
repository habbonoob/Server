using System.Text.Json.Serialization;

namespace Habbo.Camera;

/// <summary>An effect from the photo lab: its name (camera.available.effects) and strength (0-255); a frame has no
/// strength.</summary>
public sealed record RenderFilter(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("alpha")] int? Alpha);

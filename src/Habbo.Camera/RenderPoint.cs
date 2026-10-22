using System.Text.Json.Serialization;

namespace Habbo.Camera;

public sealed record RenderPoint(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y);

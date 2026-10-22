using System.Text.Json.Serialization;

namespace Habbo.Camera;

/// <summary>One column of a plane's texture: the assets stacked in it, top to bottom.</summary>
public sealed record RenderTextureColumn(
    [property: JsonPropertyName("assetNames")] IReadOnlyList<string> AssetNames);

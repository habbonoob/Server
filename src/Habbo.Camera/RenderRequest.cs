using System.Text.Json.Serialization;

namespace Habbo.Camera;

/// <summary>A photo as the client composes it (RenderRoomMessageComposer): the room's planes and the sprites in
/// view, already placed on the photo's canvas, the effects the photo lab applied, and the room.</summary>
public sealed record RenderRequest(
    [property: JsonPropertyName("planes")] IReadOnlyList<RenderPlane> Planes,
    [property: JsonPropertyName("sprites")] IReadOnlyList<RenderSprite> Sprites,
    [property: JsonPropertyName("filters")] IReadOnlyList<RenderFilter>? Filters,
    [property: JsonPropertyName("roomid")] int RoomId,
    [property: JsonPropertyName("zoom")] double Zoom = 1);

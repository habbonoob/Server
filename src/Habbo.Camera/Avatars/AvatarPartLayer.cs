namespace Habbo.Camera.Avatars;

/// <summary>A part to draw in a body part (AvatarImagePartContainer): its type and id, the colour it is tinted with when
/// it takes one, and the type it is drawn as when mirrored.</summary>
public sealed record AvatarPartLayer(string Type, string Id, uint? Color, bool IsColorable, string FlippedType);

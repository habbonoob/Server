namespace Habbo.Camera.Avatars;

/// <summary>What an avatar is drawn by, besides its parts' bitmaps: the client's geometry, part sets and default action,
/// and figuredata.</summary>
public sealed record AvatarStructure(AvatarGeometry Geometry, AvatarPartSets PartSets, AvatarAction Action, FigureData FigureData);

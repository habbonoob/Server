using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>A floor item moved by wired, and turned to its rotation. September 2026 may be told how far the item
/// overshoots and how high it arcs; no wired here moves one that way.</summary>
[WireStruct]
public sealed partial record WiredFurniMoveData(
    int FromX, int FromY, int ToX, int ToY, string FromZ, string ToZ,
    int ItemId, int AnimationMilliseconds, int Rotation,
    [property: WireSince(ClientReleases.September2026), WirePresence] int? OvershootingDistance = null,
    [property: WireSince(ClientReleases.September2026), WirePresence] int? CurveStrength = null);

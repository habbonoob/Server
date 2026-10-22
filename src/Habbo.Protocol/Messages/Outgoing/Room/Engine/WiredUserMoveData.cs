using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>A user moved by wired: walking ("mv"), or sliding in their posture ("sld"). September 2026 may be told how
/// high the move arcs; no wired here makes one arc.</summary>
[WireStruct]
public sealed partial record WiredUserMoveData(
    int FromX, int FromY, int ToX, int ToY, string FromZ, string ToZ,
    int RoomIndex, int MoveType, int AnimationMilliseconds, int BodyDirection, int HeadDirection,
    [property: WireSince(ClientReleases.September2026), WirePresence] int? JumpPower = null)
{
    public const int Walk = 0;
    public const int Slide = 1;
}

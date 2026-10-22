using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>One avatar change. The position is the tile being LEFT; the "mv" status carries
/// the tile being entered, and the head direction comes before the body. September 2026 reads how high the step
/// arcs (its jumping power) before the status: 0 slides, and nothing here makes an avatar jump.</summary>
[WireStruct]
public sealed partial record AvatarUpdateWireData(
    int RoomIndex,
    int X,
    int Y,
    string Z,
    int HeadDirection,
    int BodyDirection,
    [property: WireSince(ClientReleases.September2026)] int JumpingPower,
    string Status)
{
    /// <summary>A step that slides from tile to tile rather than arcing.</summary>
    public const int NoJump = 0;

    public static AvatarUpdateWireData From(AvatarUpdateData update) => new(
        update.RoomIndex, update.X, update.Y, update.Z,
        update.HeadDirection, update.BodyDirection, NoJump, update.Status);
}

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>830 BuildersClubPlacementWarningMessageComposer — a Builders Club placement held back for the player to
/// confirm: on the free trial, placing it hides the room from the navigator ("room.confirm.hide_room"). It echoes
/// the placement, which the client sends again, confirmed, once the player agrees: type 0 a floor item (x, y,
/// direction), 1 a wall item (its wall location).</summary>
[Message(ServerMessages.BuildersClubPlacementWarningMessageComposer)]
public sealed partial record BuildersClubPlacementWarningMessageComposer(
    int TypeCode,
    int PageId,
    int OfferId,
    string ExtraParam,
    [WireWhen("IsFloor")] int X,
    [WireWhen("IsFloor")] int Y,
    [WireWhen("IsFloor")] int Direction,
    [WireWhen("IsWall")] string WallLocation)
{
    public const int Floor = 0;
    public const int Wall = 1;

    public bool IsFloor => TypeCode == Floor;
    public bool IsWall => TypeCode != Floor;
}

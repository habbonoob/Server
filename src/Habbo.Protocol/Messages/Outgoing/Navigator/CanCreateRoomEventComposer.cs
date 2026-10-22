using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>CanCreateRoomEventComposer</summary>
[Message(ServerMessages.CanCreateRoomEventComposer)]
public sealed partial record CanCreateRoomEventComposer(bool CanCreateEvent, int ErrorCode)
{
    // navigator.cannotcreateevent.error.N
    public const int NoError = 0;
    public const int NotInRoom = 1;
    public const int NotOwner = 2;
    public const int DoorNotOpen = 3;
    public const int Disabled = 4;
    public const int RoomHasEvent = 5;
    public const int EventInAnotherRoom = 6;
}

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredLink;

/// <summary>A Link connected to a network (the client's WiredLinkConnection): the room it stands in, by id and name,
/// the Link, whether it is borrowed Builders Club furni, and whether it receives its own room's messages.</summary>
[WireStruct]
public sealed partial record WiredLinkConnectionData(
    int RoomId,
    string RoomName,
    int ItemId,
    bool IsBuildersClub,
    bool ReceiveLocal);

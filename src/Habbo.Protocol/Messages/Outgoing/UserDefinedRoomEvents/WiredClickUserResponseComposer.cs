using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>312 WiredClickUserResponseComposer — the answer to a click on a user: whether the client
/// may open the avatar menu for the user with this room index.</summary>
[Message(ServerMessages.WiredClickUserResponseComposer)]
public sealed partial record WiredClickUserResponseComposer(int RoomIndex, bool OpenMenu);

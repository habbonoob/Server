using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RoomSettings;

/// <summary>UserUnbannedFromRoomComposer</summary>
[Message(ServerMessages.UserUnbannedFromRoomComposer)]
public sealed partial record UserUnbannedFromRoomComposer(int RoomId, int UserId);

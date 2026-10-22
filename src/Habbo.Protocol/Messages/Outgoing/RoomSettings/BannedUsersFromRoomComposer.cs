using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RoomSettings;

/// <summary>BannedUsersFromRoomComposer</summary>
[Message(ServerMessages.BannedUsersFromRoomComposer)]
public sealed partial record BannedUsersFromRoomComposer(int RoomId, IReadOnlyList<BannedUserData> BannedUsers);

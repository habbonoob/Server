using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>2073 RemoveOwnRoomRightsRoom — the user gives up their own rights in the room.</summary>
[Incoming(ClientMessages.RemoveOwnRoomRightsRoomMessageEvent)]
public sealed partial record RemoveOwnRoomRightsRoomMessageEvent(int RoomId);

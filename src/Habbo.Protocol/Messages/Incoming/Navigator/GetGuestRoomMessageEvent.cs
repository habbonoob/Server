using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>3176 GetGuestRoom — the room's details, and whether the client is entering it.</summary>
[Incoming(ClientMessages.GetGuestRoomMessageEvent)]
public sealed partial record GetGuestRoomMessageEvent(int RoomId, int EnterRoom, int ForwardRoom);

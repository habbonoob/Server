using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.RoomSettings;

/// <summary>GetCustomRoomFilterMessageEvent</summary>
[Incoming(ClientMessages.GetCustomRoomFilterMessageEvent)]
public sealed partial record GetCustomRoomFilterMessageEvent(int RoomId);

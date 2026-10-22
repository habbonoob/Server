using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.RoomSettings;

/// <summary>UpdateRoomFilterMessageEvent</summary>
[Incoming(ClientMessages.UpdateRoomFilterMessageEvent)]
public sealed partial record UpdateRoomFilterMessageEvent(int RoomId, bool Add, string Word);

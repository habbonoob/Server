using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.RoomDirectory;

/// <summary>RoomNetworkOpenConnectionMessageEvent</summary>
[Incoming(ClientMessages.RoomNetworkOpenConnectionMessageEvent)]
public sealed partial record RoomNetworkOpenConnectionMessageEvent(int NetworkId, int HomeRoomId);

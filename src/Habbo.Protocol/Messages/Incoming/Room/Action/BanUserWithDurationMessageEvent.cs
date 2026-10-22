using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Action;

/// <summary>BanUserWithDurationMessageEvent</summary>
[Incoming(ClientMessages.BanUserWithDurationMessageEvent)]
public sealed partial record BanUserWithDurationMessageEvent(int UserId, int RoomId, string Duration);

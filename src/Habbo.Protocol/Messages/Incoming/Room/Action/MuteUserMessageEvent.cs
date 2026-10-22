using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Action;

/// <summary>MuteUserMessageEvent</summary>
[Incoming(ClientMessages.MuteUserMessageEvent)]
public sealed partial record MuteUserMessageEvent(int UserId, int RoomId, int Minutes);

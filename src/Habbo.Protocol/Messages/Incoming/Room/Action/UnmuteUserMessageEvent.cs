using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Action;

/// <summary>UnmuteUserMessageEvent</summary>
[Incoming(ClientMessages.UnmuteUserMessageEvent)]
public sealed partial record UnmuteUserMessageEvent(int UserId, int RoomId);

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>GetRoomVisitsMessageEvent</summary>
[Incoming(ClientMessages.GetRoomVisitsMessageEvent)]
public sealed partial record GetRoomVisitsMessageEvent(int UserId);

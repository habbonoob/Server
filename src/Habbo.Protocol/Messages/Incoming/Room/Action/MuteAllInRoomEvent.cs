using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Action;

/// <summary>MuteAllInRoomEvent</summary>
[Incoming(ClientMessages.MuteAllInRoomEvent)]
public sealed partial record MuteAllInRoomEvent;

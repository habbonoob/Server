using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RoomSettings;

/// <summary>MuteAllInRoomComposer</summary>
[Message(ServerMessages.MuteAllInRoomComposer)]
public sealed partial record MuteAllInRoomComposer(bool AllMuted);

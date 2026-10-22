using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Action;

/// <summary>2801 DanceMessageComposer — an avatar starts a dance, or (0) stops.</summary>
[Message(ServerMessages.DanceMessageComposer)]
public sealed partial record DanceMessageComposer(int RoomIndex, int Style);

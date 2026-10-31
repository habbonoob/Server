using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>RoomUseHabbicon (September 2026) — an avatar shows a Habbicon: the room draws it in a bubble over them,
/// the one who sent it included.</summary>
[Message(ServerMessages.RoomUseHabbiconComposer)]
public sealed partial record RoomUseHabbiconComposer(int RoomIndex, int HabbiconId);

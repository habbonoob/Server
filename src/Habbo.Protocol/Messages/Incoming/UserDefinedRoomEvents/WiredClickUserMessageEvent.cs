using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;

/// <summary>2992 WiredClickUser — the user clicked the avatar with this room index, in a room whose
/// wired said it listens for that.</summary>
[Incoming(ClientMessages.WiredClickUserMessageEvent)]
public sealed partial record WiredClickUserMessageEvent(int RoomIndex);

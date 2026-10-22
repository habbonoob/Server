using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>3001 RoomEntryInfoMessageComposer.</summary>
[Message(ServerMessages.RoomEntryInfoMessageComposer)]
public sealed partial record RoomEntryInfoMessageComposer(int RoomId, bool IsOwner);

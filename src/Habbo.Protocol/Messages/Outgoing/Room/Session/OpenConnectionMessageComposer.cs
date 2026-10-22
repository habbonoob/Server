using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Session;

/// <summary>547 OpenConnectionMessageComposer.</summary>
[Message(ServerMessages.OpenConnectionMessageComposer)]
public sealed partial record OpenConnectionMessageComposer(int RoomId);

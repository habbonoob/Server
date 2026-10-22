using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Permissions;

/// <summary>2587 YouAreControllerMessageComposer.</summary>
[Message(ServerMessages.YouAreControllerMessageComposer)]
public sealed partial record YouAreControllerMessageComposer(int RoomId, int RoomControllerLevel);

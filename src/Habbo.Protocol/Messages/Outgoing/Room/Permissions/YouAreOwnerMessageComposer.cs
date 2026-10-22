using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Permissions;

/// <summary>2540 YouAreOwnerMessageComposer.</summary>
[Message(ServerMessages.YouAreOwnerMessageComposer)]
public sealed partial record YouAreOwnerMessageComposer(int RoomId);

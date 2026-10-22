using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Permissions;

/// <summary>242 YouAreNotControllerMessageComposer — the user no longer has rights in the room.</summary>
[Message(ServerMessages.YouAreNotControllerMessageComposer)]
public sealed partial record YouAreNotControllerMessageComposer(int RoomId);

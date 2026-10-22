using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>2519 UsersMessageComposer — every avatar currently in the room.</summary>
[Message(ServerMessages.UsersMessageComposer)]
public sealed partial record UsersMessageComposer(IReadOnlyList<RoomEntityWireData> Entities)
{
    public static UsersMessageComposer From(IReadOnlyList<RoomEntityData> entities) =>
        new(entities.Select(RoomEntityWireData.From).ToList());
}

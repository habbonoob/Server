using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>1818 ObjectsMessageComposer — floor items, preceded by the owner name map.</summary>
[Message(ServerMessages.ObjectsMessageComposer)]
public sealed partial record ObjectsMessageComposer(
    IReadOnlyList<RoomOwnerData> Owners, IReadOnlyList<FloorItemData> Items);

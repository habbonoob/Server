using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>437 ItemsMessageComposer — wall items, preceded by the owner name map.</summary>
[Message(ServerMessages.ItemsMessageComposer)]
public sealed partial record ItemsMessageComposer(
    IReadOnlyList<RoomOwnerData> Owners, IReadOnlyList<WallItemData> Items);

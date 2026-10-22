using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>345 ClickFurni — the user clicked an item (a wall item's id negated); the type is 0.</summary>
[Incoming(ClientMessages.ClickFurniMessageEvent)]
public sealed partial record ClickFurniMessageEvent(int ItemId, int Type);

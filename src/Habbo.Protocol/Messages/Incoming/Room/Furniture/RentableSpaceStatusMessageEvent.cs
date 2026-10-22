using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>RentableSpaceStatusMessageEvent — who rents a space, and whether the player may</summary>
[Incoming(ClientMessages.RentableSpaceStatusMessageEvent)]
public sealed partial record RentableSpaceStatusMessageEvent(int ItemId);

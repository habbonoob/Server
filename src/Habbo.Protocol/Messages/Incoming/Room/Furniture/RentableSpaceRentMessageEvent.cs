using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>RentableSpaceRentMessageEvent — rent a space</summary>
[Incoming(ClientMessages.RentableSpaceRentMessageEvent)]
public sealed partial record RentableSpaceRentMessageEvent(int ItemId);

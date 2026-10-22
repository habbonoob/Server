using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>RentableSpaceCancelRentMessageEvent — end a space's rent</summary>
[Incoming(ClientMessages.RentableSpaceCancelRentMessageEvent)]
public sealed partial record RentableSpaceCancelRentMessageEvent(int ItemId);

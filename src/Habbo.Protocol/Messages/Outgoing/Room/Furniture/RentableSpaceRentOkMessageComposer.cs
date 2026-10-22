using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>RentableSpaceRentOkMessageComposer — the space is rented, for this many seconds</summary>
[Message(ServerMessages.RentableSpaceRentOkMessageComposer)]
public sealed partial record RentableSpaceRentOkMessageComposer(int ExpiryTime);

using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>RentableSpaceStatusMessageComposer — who rents a space, and whether the player may</summary>
[Message(ServerMessages.RentableSpaceStatusMessageComposer)]
public sealed partial record RentableSpaceStatusMessageComposer(bool Rented, int CanRentErrorCode, int RenterId, string RenterName, int TimeRemaining, int Price);

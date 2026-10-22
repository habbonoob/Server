using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>RentableSpaceRentFailedMessageComposer — why the space could not be rented. The same codes say,
/// in the status, why the player may not rent it.</summary>
[Message(ServerMessages.RentableSpaceRentFailedMessageComposer)]
public sealed partial record RentableSpaceRentFailedMessageComposer(int Reason)
{
    public const int None = 0;
    public const int AlreadyRented = 100;
    public const int NotRented = 101;
    public const int NotRentedByYou = 102;
    public const int OnlyOneSpace = 103;
    public const int NotEnoughCredits = 200;
    public const int NoPermission = 202;
    public const int ClubRequired = 203;
    public const int Disabled = 300;
    public const int Generic = 400;
}

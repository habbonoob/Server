using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Pets;

/// <summary>GoToBreedingNestFailureComposer — why a pet cannot go to a breeding nest</summary>
[Message(ServerMessages.GoToBreedingNestFailureComposer)]
public sealed partial record GoToBreedingNestFailureComposer(int Reason)
{
    public const int NoNests = 0;
    public const int NoSuitableNest = 1;
    public const int NestFull = 2;

    /// <summary>The second pet in a nest has to belong to the nest's owner.</summary>
    public const int NotNestOwner = 3;

    public const int AlreadyInNest = 4;
    public const int NoWayToNest = 5;
    public const int TooTired = 6;
}

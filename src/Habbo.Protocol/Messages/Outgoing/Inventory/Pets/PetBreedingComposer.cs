using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Pets;

/// <summary>PetBreedingComposer — a request to breed plants, or what became of it. Own pet first, as the
/// receiver sees it.</summary>
[Message(ServerMessages.PetBreedingComposer)]
public sealed partial record PetBreedingComposer(int State, int OwnPetId, int OtherPetId)
{
    /// <summary>The other side turned it down.</summary>
    public const int Cancelled = 1;

    /// <summary>The other side agreed.</summary>
    public const int Accepted = 2;

    /// <summary>Someone asks to breed their plant with the receiver's.</summary>
    public const int Requested = 3;
}

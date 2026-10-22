using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Pets;

/// <summary>ConfirmBreedingResultComposer — whether the pets in the nest were bred, and why not</summary>
[Message(ServerMessages.ConfirmBreedingResultComposer)]
public sealed partial record ConfirmBreedingResultComposer(int NestId, int Result)
{
    public const int Bred = 0;
    public const int NoNest = 1;
    public const int PetsMissing = 2;
    public const int NameInvalid = 3;
}

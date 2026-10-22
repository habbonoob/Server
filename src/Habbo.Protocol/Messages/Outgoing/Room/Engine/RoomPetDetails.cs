using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>The trailing fields a pet carries: its kind, owner, and what it is up to.</summary>
[WireStruct]
public sealed partial record RoomPetDetails(
    int SubType,
    int OwnerId,
    string OwnerName,
    int RarityLevel,
    bool HasSaddle,
    bool IsRiding,
    bool CanBreed,
    bool CanHarvest,
    bool CanRevive,
    bool HasBreedingPermission,
    int PetLevel,
    string PetPosture);

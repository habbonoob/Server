using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Pets;

/// <summary>PetStatusUpdateComposer — what can be done with a pet changed</summary>
[Message(ServerMessages.PetStatusUpdateComposer)]
public sealed partial record PetStatusUpdateComposer(int RoomIndex, int PetId, bool CanBreed, bool CanHarvest, bool CanRevive, bool HasBreedingPermission);

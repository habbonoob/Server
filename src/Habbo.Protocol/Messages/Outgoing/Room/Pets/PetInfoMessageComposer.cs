using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Pets;

/// <summary>PetInfoMessageComposer — a pet's details</summary>
[Message(ServerMessages.PetInfoMessageComposer)]
public sealed partial record PetInfoMessageComposer(int PetId, string Name, int Level, int MaxLevel, int Experience, int ExperienceRequiredToLevel, int Energy, int MaxEnergy, int Nutrition, int MaxNutrition, int Respect, int OwnerId, int Age, string OwnerName, int BreedId, bool HasFreeSaddle, bool IsRiding, IReadOnlyList<int> SkillThresholds, int AccessRights, bool CanBreed, bool CanHarvest, bool CanRevive, int RarityLevel, int MaxWellBeingSeconds, int RemainingWellBeingSeconds, int RemainingGrowingSeconds, bool HasBreedingPermission);

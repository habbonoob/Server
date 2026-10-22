using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Pets;

/// <summary>PetBreedingResultData — a seed two plants gave, and who got it</summary>
[WireStruct]
public sealed partial record PetBreedingResultData(int StuffId, int ClassId, string ProductCode, int UserId, string UserName, int RarityLevel, bool HasMutation)
{
    /// <summary>The stuff id of a seed nobody got.</summary>
    public const int NoSeed = -1;

    public static PetBreedingResultData None { get; } = new(NoSeed, 0, string.Empty, 0, string.Empty, 0, false);
}

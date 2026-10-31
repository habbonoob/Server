namespace Habbo.Silo.Wired;

/// <summary>What the room knows of a pet, as its info card shows it, for the ~pet and ~plant variables.</summary>
public sealed record WiredPetStats(
    int Level, int MaxLevel, int Experience, int ExperienceRequired, int Energy, int MaxEnergy, int Happiness, int MaxHappiness,
    int Scratches, int OwnerAccountId, DateTimeOffset CreatedAt, int Rarity)
{
    /// <summary>For a monster plant: how it grows and how well it is; null for any other pet.</summary>
    public WiredPlantStats? Plant { get; init; }
}

public sealed record WiredPlantStats(
    bool CanBreed, bool CanHarvest, bool CanRevive, bool IsDead, bool IsGrowing,
    int MaxWellbeingSeconds, int RemainingWellbeingSeconds, int RemainingGrowingSeconds, int Color, int Shape);

/// <summary>What a transaction under a contract moved, for the @event.transaction_complete variables.</summary>
public sealed record WiredTransactionDone(int Multiplier, int DepositedFurni, int DepositedCoins, int WithdrawnFurni, int WithdrawnCoins);

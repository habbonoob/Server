using Habbo.Domain.Pets;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Pets;

/// <summary>How pets grow: the experience each level takes, the energy and nutrition a level allows, and
/// how both move on with time.</summary>
public sealed class PetRules(IOptions<PetOptions> options)
{
    private PetOptions Rules => options.Value;

    public int MaxLevel => Rules.LevelExperience.Count + 1;

    /// <summary>The experience the pet needs for its next level; at the top, what the top took.</summary>
    public int ExperienceForNextLevel(int level) =>
        Rules.LevelExperience.Count == 0 ? 0 : Rules.LevelExperience[Math.Clamp(level - 1, 0, Rules.LevelExperience.Count - 1)];

    /// <summary>The level a pet with this much experience has reached.</summary>
    public int LevelFor(int experience) => 1 + Rules.LevelExperience.Count(needed => experience >= needed);

    public int MaxEnergy(int level) => Rules.EnergyBase + Rules.EnergyPerLevel * (level - 1);

    public int MaxNutrition(int level) => Rules.NutritionBase + Rules.NutritionPerLevel * (level - 1);

    public int Energy(Pet pet, DateTimeOffset now) =>
        Math.Clamp(pet.Energy + (int)(MinutesSince(pet, now) * Rules.EnergyRegainedPerMinute), 0, MaxEnergy(pet.Level));

    public int Nutrition(Pet pet, DateTimeOffset now) =>
        Math.Clamp(pet.Nutrition - (int)(MinutesSince(pet, now) * Rules.NutritionLostPerMinute), 0, MaxNutrition(pet.Level));

    /// <summary>Brings the pet's energy and nutrition up to now, so a change can be made to them.</summary>
    public void Settle(Pet pet, DateTimeOffset now)
    {
        (pet.Energy, pet.Nutrition) = (Energy(pet, now), Nutrition(pet, now));
        pet.VitalsAt = now;
    }

    /// <summary>A new pet starts full of energy and fed.</summary>
    public void Start(Pet pet, DateTimeOffset now)
    {
        pet.Level = 1;
        pet.Energy = MaxEnergy(1);
        pet.Nutrition = MaxNutrition(1);
        pet.VitalsAt = now;
    }

    public static int AgeInDays(Pet pet, DateTimeOffset now) => (int)(now - pet.CreatedAt).TotalDays;

    private static double MinutesSince(Pet pet, DateTimeOffset now) => Math.Max(0, (now - pet.VitalsAt).TotalMinutes);
}

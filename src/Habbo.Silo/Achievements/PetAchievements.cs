namespace Habbo.Silo.Achievements;

/// <summary>The pets with achievements of their own, by pet type: levelling up, being scratched, eating. Every pet also
/// counts towards the general pet achievements. (The monkey's level-up achievement, Lost Monkey, came from a game of its
/// own, not from the pet.)</summary>
public static class PetAchievements
{
    public const int MonsterPlant = 16;
    private const int Gnome = 26;
    private const int Leprechaun = 27;
    private const int ObbahWobbah = 31;

    private static readonly Dictionary<int, (string? LevelUp, string? RespectGiver, string? Feeding)> OwnAchievements = new()
    {
        [Gnome] = ("ACH_GnomeLevelUp", "ACH_GnomeRespectGiver", "ACH_GnomeFeeding"),
        [Leprechaun] = ("ACH_LeprechaunLevelUp", "ACH_LeprechaunRespectGiver", "ACH_LeprechaunFeeding"),
        [ObbahWobbah] = ("ACH_HaloompaLevelUp", "ACH_HaloompaRespectGiver", "ACH_HaloompaFeeding"),
    };

    /// <summary>The baby pets the breeder achievements count, each bred in a breeding box (Habbox wiki).</summary>
    public static readonly IReadOnlyList<(int BabyType, string Achievement)> Breeders =
    [
        (24, "ACH_BearBreeder"),
        (25, "ACH_TerrierBreeder"),
        (28, "ACH_CatBreeder"),
        (29, "ACH_DogBreeder"),
        (30, "ACH_PigBreeder"),
    ];

    public static IEnumerable<string> LevelUp(int petType) =>
        [AchievementNames.PetLevelUp, .. OwnAchievements.GetValueOrDefault(petType).LevelUp is { } own ? new[] { own } : []];

    public static IEnumerable<string> RespectGiver(int petType) =>
        [AchievementNames.PetRespectGiver, .. OwnAchievements.GetValueOrDefault(petType).RespectGiver is { } own ? new[] { own } : []];

    public static IEnumerable<string> Feeding(int petType) =>
        [AchievementNames.PetFeeding, .. OwnAchievements.GetValueOrDefault(petType).Feeding is { } own ? new[] { own } : []];
}

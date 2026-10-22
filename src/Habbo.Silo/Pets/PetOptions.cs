namespace Habbo.Silo.Pets;

/// <summary>Pets (config section "Pets").</summary>
public sealed class PetOptions
{
    /// <summary>Whether the hotel lets pets into rooms at all.</summary>
    public bool Enabled { get; set; }

    /// <summary>How many pets one room may hold, and how many of them one player's.</summary>
    public int MaxPerRoom { get; set; }
    public int MaxOwnPerRoom { get; set; }

    public int NameMinLength { get; set; }
    public int NameMaxLength { get; set; }

    /// <summary>The characters a pet's name may use, as a regular expression character class.</summary>
    public string NameCharacters { get; set; } = string.Empty;

    /// <summary>The experience a pet needs to reach each level after the first; the last is the top level.</summary>
    public List<int> LevelExperience { get; set; } = [];

    /// <summary>Energy and nutrition at the first level, and what each level after adds.</summary>
    public int EnergyBase { get; set; }
    public int EnergyPerLevel { get; set; }
    public int NutritionBase { get; set; }
    public int NutritionPerLevel { get; set; }

    /// <summary>How fast a pet gets its energy back, and how fast it grows hungry.</summary>
    public int EnergyRegainedPerMinute { get; set; }
    public int NutritionLostPerMinute { get; set; }

    /// <summary>How old an account must be to respect pets, and what a respect gives the pet.</summary>
    public int RespectRequiredAccountDays { get; set; }
    public int RespectExperience { get; set; }

    /// <summary>How long a pet left to itself stands about before it wanders off again.</summary>
    public int WanderPauseSeconds { get; set; }

    /// <summary>The client's colour tags by number (its PetColorResult), which a dye names.</summary>
    public List<string> ColorTags { get; set; } = [];

    /// <summary>The postures a pet eats and drinks in, and rests in, the first its kind has.</summary>
    public List<string> FeedPostures { get; set; } = [];
    public List<string> RestPostures { get; set; } = [];

    /// <summary>How long a bite, a sip and a rest take, and what each gives.</summary>
    public int EatSeconds { get; set; }
    public int DrinkSeconds { get; set; }
    public int RestSeconds { get; set; }
    public int FoodNutrition { get; set; }
    public int DrinkEnergy { get; set; }

    /// <summary>What something handed to a pet to eat gives it.</summary>
    public int HandItemNutrition { get; set; }

    /// <summary>Below what share of its most a pet goes to eat, or to rest, by itself; in percent.</summary>
    public int HungryBelowPercent { get; set; }

    /// <summary>The energy a pet needs to go to a breeding nest, which breeding uses up.</summary>
    public int BreedingEnergy { get; set; }
    public int TiredBelowPercent { get; set; }
}

namespace Habbo.Silo.Pets;

/// <summary>Monster plants (config section "Plants").</summary>
public sealed class PlantOptions
{
    /// <summary>The kind of pet monster plants are.</summary>
    public int TypeId { get; set; }

    /// <summary>The game text a new plant is named by.</summary>
    public string NameTextKey { get; set; } = string.Empty;

    /// <summary>How long a plant takes to grow from a seed.</summary>
    public int GrowSeconds { get; set; }

    /// <summary>How long a plant stays well without water, and below what share of that it looks ill; in percent.</summary>
    public int WellBeingSeconds { get; set; }
    public int IllBelowPercent { get; set; }

    /// <summary>How much growing light spares a plant.</summary>
    public int LightGrowthSeconds { get; set; }

    /// <summary>The seed two plants give, what a harvested plant becomes, and what a composted one leaves behind.</summary>
    public string SeedClassName { get; set; } = string.Empty;
    public string HarvestClassName { get; set; } = string.Empty;
    public string CompostClassName { get; set; } = string.Empty;

    /// <summary>The chance, in percent, that breeding gives a second seed, and that a trait of a bred seed mutates.</summary>
    public int SecondSeedChance { get; set; }
    public int MutationChance { get; set; }

    /// <summary>The rarest plant each rebreeding potion works on, by the potion's class name.</summary>
    public Dictionary<string, int> RebreedMaxRarity { get; set; } = [];
}

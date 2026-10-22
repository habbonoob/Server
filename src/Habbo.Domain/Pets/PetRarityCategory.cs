namespace Habbo.Domain.Pets;

/// <summary>How rare a baby bred in a nest may turn out: a category of a baby kind's breeds, rarest first
/// by rank, with its chance in percent and what each level of the parents adds to it.</summary>
public sealed class PetRarityCategory
{
    public int BabyTypeId { get; set; }

    /// <summary>0 is the rarest.</summary>
    public int Rank { get; set; }

    public double Chance { get; set; }

    public double ChancePerLevel { get; set; }

    /// <summary>The palettes (breeds) in the category.</summary>
    public List<int> Palettes { get; set; } = [];
}

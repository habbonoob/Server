namespace Habbo.Domain.Recycler;

/// <summary>A level of the Furni-Matic's prizes: one in its denominator draws it.</summary>
public sealed class RecyclerPrizeLevel
{
    public int Id { get; set; }

    public int ProbabilityDenominator { get; set; }

    public List<RecyclerPrize> Prizes { get; set; } = [];
}

using Habbo.Domain.Furniture.Chests;

namespace Habbo.Silo.Chests;

/// <summary>A contract as one transaction runs it: what it asks and gives, how many times the rules are to be met
/// (once, exactly so many, or as often as offered up to a limit), and the chests it pays into and gives from.</summary>
public sealed record ContractTerms(
    WiredContract Contract,
    int RuleType,
    int Times,
    IReadOnlyList<int> ChestIds,
    int ContractItemId)
{
    /// <summary>How the rules are met, as the initiate effect's dialog numbers it.</summary>
    public const int Normal = 0;
    public const int Multiplier = 1;
    public const int AutoMultiplier = 2;

    /// <summary>A payment that takes anything, as a donation.</summary>
    public bool IsDonation => Contract.Type == WiredContract.Payment && Contract.PaymentMode == WiredContract.DonationMode;

    /// <summary>The most times the rules may be met.</summary>
    public int MaxTimes => RuleType == Normal ? 1 : Math.Max(1, Times);
}

namespace Habbo.Silo.Wired.Conditions;

/// <summary>The "lower than / equals / higher than" choice (wiredfurni.params.comparison.N), and
/// the variable dialogs' fuller set (&lt;, =, &gt;, ≤, ≠, ≥).</summary>
public static class WiredComparison
{
    public const int LowerThan = 0;
    public const int EqualTo = 1;
    public const int HigherThan = 2;
    public const int LowerOrEqual = 3;
    public const int NotEqualTo = 4;
    public const int HigherOrEqual = 5;

    public static bool Holds(int comparison, int value, int against) => comparison switch
    {
        LowerThan => value < against,
        HigherThan => value > against,
        LowerOrEqual => value <= against,
        NotEqualTo => value != against,
        HigherOrEqual => value >= against,
        _ => value == against,
    };
}

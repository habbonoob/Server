namespace Habbo.Silo.Wired.Rewards;

/// <summary>One of a give reward box's rewards: a badge or a product, by its code, and the chance in a hundred of
/// drawing it.</summary>
public sealed record WiredReward(bool IsBadge, string Code, int Probability);

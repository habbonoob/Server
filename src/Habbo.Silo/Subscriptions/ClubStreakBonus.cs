namespace Habbo.Silo.Subscriptions;

/// <summary>A payday bonus for a membership that has run unbroken at least <see cref="Days"/>.</summary>
public sealed class ClubStreakBonus
{
    public int Days { get; set; }

    public int Credits { get; set; }
}

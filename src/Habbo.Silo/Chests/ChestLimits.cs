namespace Habbo.Silo.Chests;

/// <summary>How much chests hold and what more costs, as the client's variables set them (wired.furni_chest.*,
/// wired.coins_chest.*, wired.chests.*), so the server and the chest window agree.</summary>
public sealed record ChestLimits(
    int FurniInitialCapacity,
    int FurniUpgradeCapacity,
    int FurniMaxUpgrades,
    int FurniStarterCapacity,
    int CoinsInitialCapacity,
    int CoinsUpgradeCapacity,
    int CoinsMaxUpgrades,
    int CoinsStarterCapacity,
    int UpgradeCostCredits,
    int UpgradeCostDiamonds,
    string StarterInfix,
    int MaxLogs)
{
    /// <summary>The activity point type an upgrade's diamonds are paid in.</summary>
    public const int DiamondsType = 5;

    public bool IsStarter(string className) => StarterInfix.Length > 0 && className.Contains(StarterInfix, StringComparison.Ordinal);

    public int MaxUpgrades(bool coins) => coins ? CoinsMaxUpgrades : FurniMaxUpgrades;

    /// <summary>The most the chest may hold: a starter chest's fixed amount, or the initial plus each upgrade bought.</summary>
    public int MaxCapacity(bool coins, bool starter, int level) =>
        starter ? coins ? CoinsStarterCapacity : FurniStarterCapacity
        : coins ? CoinsInitialCapacity + CoinsUpgradeCapacity * level
        : FurniInitialCapacity + FurniUpgradeCapacity * level;
}

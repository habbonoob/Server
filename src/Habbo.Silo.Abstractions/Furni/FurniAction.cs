namespace Habbo.Silo.Abstractions.Furni;

/// <summary>What a player asked an item to do. Each comes from its own client message, and
/// which ones an item answers is up to its behaviour.</summary>
public enum FurniAction
{
    /// <summary>UseFurniture / UseWallItem: a double-click, or the infostand's Use button.</summary>
    Use,

    /// <summary>SetRandomState: a double-click on a furniture_randomstate item.</summary>
    UseRandom,

    /// <summary>ThrowDice.</summary>
    ThrowDice,

    /// <summary>DiceOff.</summary>
    DiceOff,

    /// <summary>CreditFurniRedeem: exchange the item for what it is worth in credits.</summary>
    Redeem,

    /// <summary>SpinWheelOfFortune.</summary>
    Spin,
}

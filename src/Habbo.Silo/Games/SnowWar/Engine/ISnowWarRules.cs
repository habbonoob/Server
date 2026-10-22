namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>What the game tells whoever keeps its statistics; none of it changes the game.</summary>
internal interface ISnowWarRules
{
    void SnowballMade(HumanGameObject human);

    void Hit(HumanGameObject victim, HumanGameObject thrower);

    void KnockedDown(HumanGameObject victim, HumanGameObject thrower);

    /// <summary>A player took snowballs from a machine or a pile; counted as they land in the hand, not when asked for.</summary>
    void SnowballsFromMachine(HumanGameObject human, int count)
    {
    }
}

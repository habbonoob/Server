using Habbo.Silo.Games.SnowWar.Engine;

namespace Habbo.Silo.Games.SnowWar;

/// <summary>Counts what happens in a game, player by player (by their game object), for the results.</summary>
internal sealed class SnowWarGameRules : ISnowWarRules
{
    private readonly Dictionary<int, SnowWarPlayerStats> _stats = [];

    public SnowWarPlayerStats Of(int humanId) =>
        _stats.TryGetValue(humanId, out var stats) ? stats : _stats[humanId] = new SnowWarPlayerStats();

    public void SnowballMade(HumanGameObject human) => Of(human.Id).SnowballsCreated++;

    public void Hit(HumanGameObject victim, HumanGameObject thrower)
    {
        Of(thrower.Id).SnowballHits++;
        Of(victim.Id).SnowballHitsTaken++;
    }

    public void KnockedDown(HumanGameObject victim, HumanGameObject thrower)
    {
        Of(thrower.Id).Kills++;
        Of(victim.Id).Deaths++;
    }

    public void SnowballsFromMachine(HumanGameObject human, int count) => Of(human.Id).SnowballsFromMachine += count;
}

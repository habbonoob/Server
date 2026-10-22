namespace Habbo.Silo.Games.SnowWar;

/// <summary>What one player did in a game, as the results show it.</summary>
public sealed class SnowWarPlayerStats
{
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int SnowballHits { get; set; }
    public int SnowballHitsTaken { get; set; }
    public int SnowballsThrown { get; set; }
    public int SnowballsCreated { get; set; }
    public int SnowballsFromMachine { get; set; }
}

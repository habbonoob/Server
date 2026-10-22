namespace Habbo.Silo.Games.SnowWar.Arenas;

/// <summary>What a furni is in the game: a tree (so many hits), a pile or a machine (so many snowballs).</summary>
public sealed class SnowWarGameObjectSeed
{
    public const string Tree = "tree";
    public const string Pile = "pile";
    public const string Machine = "machine";

    public string Type { get; set; } = string.Empty;
    public int MaxHits { get; set; }
    public int Snowballs { get; set; }
    public int MaxSnowballs { get; set; }
}

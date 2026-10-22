namespace Habbo.Silo.Games.SnowWar.Arenas;

/// <summary>A furni of an arena, and the game object it is, if any.</summary>
public sealed class SnowWarFurniSeed
{
    public string ClassName { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Direction { get; set; }
    public SnowWarGameObjectSeed? GameObject { get; set; }
}

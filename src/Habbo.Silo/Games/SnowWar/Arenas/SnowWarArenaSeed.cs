namespace Habbo.Silo.Games.SnowWar.Arenas;

/// <summary>One arena as the seed writes it: its field type, its floor row by row, its furni and where each team
/// starts (tile x and y, by team number).</summary>
public sealed class SnowWarArenaSeed
{
    public int FieldType { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public List<string> HeightMap { get; set; } = [];
    public List<SnowWarFurniSeed> Furni { get; set; } = [];
    public Dictionary<string, List<int[]>> Spawns { get; set; } = [];
}

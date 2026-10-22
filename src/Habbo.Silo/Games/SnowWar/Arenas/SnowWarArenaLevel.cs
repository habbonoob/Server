namespace Habbo.Silo.Games.SnowWar.Arenas;

/// <summary>An arena ready to play on: its floor, its furni with their sizes, and the starting tiles of each team.</summary>
public sealed record SnowWarArenaLevel(
    int FieldType, int Width, int Height, string HeightMap, IReadOnlyList<SnowWarLevelFurni> Furni,
    IReadOnlyDictionary<int, IReadOnlyList<(int X, int Y)>> Spawns);

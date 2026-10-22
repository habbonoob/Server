namespace Habbo.Silo.Games.SnowWar.Arenas;

/// <summary>A furni of a loaded arena: its id on the field, its class, where it stands and how it is built, in game
/// units, and the game object it is, if any.</summary>
public sealed record SnowWarLevelFurni(
    int Id, string ClassName, int X, int Y, int XDimension, int YDimension, int Height, int Direction, int Altitude,
    bool CanStandOn, SnowWarGameObjectSeed? GameObject);

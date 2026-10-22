namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>A furni of the arena, as the game sees it: where it stands, what it covers, how high it is (in game units)
/// and whether one may stand on it.</summary>
internal sealed record SnowWarFuseObject(int Id, int X, int Y, int XDimension, int YDimension, int Height, int Direction, bool CanStandOn);

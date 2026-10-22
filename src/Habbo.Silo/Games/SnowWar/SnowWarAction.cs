namespace Habbo.Silo.Games.SnowWar;

/// <summary>A player's request, kept until the next turn is decided: where to (in game units), at whom, how thrown.</summary>
public sealed record SnowWarAction(Guid PlayerId, SnowWarActionKind Kind, int X = 0, int Y = 0, int Target = 0, int Trajectory = 0);

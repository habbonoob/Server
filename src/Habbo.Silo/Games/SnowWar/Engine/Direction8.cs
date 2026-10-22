namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>One of the eight directions, 0 north clockwise to 7 north-west, as the client numbers them.</summary>
internal sealed class Direction8
{
    public static readonly Direction8 N = new(0, 0, -1);
    public static readonly Direction8 NE = new(1, 1, -1);
    public static readonly Direction8 E = new(2, 1, 0);
    public static readonly Direction8 SE = new(3, 1, 1);
    public static readonly Direction8 S = new(4, 0, 1);
    public static readonly Direction8 SW = new(5, -1, 1);
    public static readonly Direction8 W = new(6, -1, 0);
    public static readonly Direction8 NW = new(7, -1, -1);

    private static readonly Direction8[] All = [N, NE, E, SE, S, SW, W, NW];

    private Direction8(int value, int unitX, int unitY)
    {
        Value = value;
        UnitX = unitX;
        UnitY = unitY;
    }

    public int Value { get; }
    public int UnitX { get; }
    public int UnitY { get; }

    public static Direction8? Get(int value) => value is < 0 or > 7 ? null : All[value];

    public Direction8 Rotate(int steps) => All[(Value + steps) & 7];

    public Direction8 Opposite() => Rotate(4);

    public Direction8 Rotate45(bool clockwise) => Rotate(clockwise ? 1 : -1);
}

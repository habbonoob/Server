namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>A direction in whole degrees, 0 north clockwise, with the client's table-driven conversions.</summary>
internal sealed class Direction360(int value)
{
    public int Value { get; private set; } = value;

    public void Set(int value) => Value = Validate(value);

    public Direction8 ToDirection8() => ToDirection8(Value);

    public int BaseVectorX => SnowWarTables.BaseVectorX[Value];
    public int BaseVectorY => SnowWarTables.BaseVectorY[Value];

    /// <summary>Into 0..359 as the client does it: a negative multiple of 360 comes out as 360, not 0.</summary>
    public static int Validate(int value)
    {
        if (value > 359)
            return value % 360;
        if (value < 0)
            return 360 + value % 360;
        return value;
    }

    public static Direction8 ToDirection8(int value) => Direction8.Get(((Validate(value - 22) / 45) + 1) & 7)!;

    public static int FromDirection8(Direction8 direction) => direction.Value * 45;

    public static int BaseVectorXOf(int value) => SnowWarTables.BaseVectorX[Validate(value)];
    public static int BaseVectorYOf(int value) => SnowWarTables.BaseVectorY[Validate(value)];

    /// <summary>The angle of a vector, from the steeper component over the other and the client's angle table.</summary>
    public static int AngleFromComponents(int x, int y)
    {
        var table = SnowWarTables.ComponentToAngle;
        int slope;
        if (SnowWarMath.Abs(x) <= SnowWarMath.Abs(y))
        {
            if (y == 0)
                y = 1;
            x *= 256;
            slope = Math.Min(SnowWarMath.Abs(x / y), 255);
            if (y < 0)
                return x > 0 ? table[slope] : 360 - table[slope];
            return x > 0 ? 180 - table[slope] : 180 + table[slope];
        }

        if (x == 0)
            x = 1;
        y *= 256;
        slope = Math.Min(SnowWarMath.Abs(y / x), 255);
        if (y < 0)
            return x > 0 ? 90 - table[slope] : 270 + table[slope];
        return x > 0 ? 90 + table[slope] : 270 - table[slope];
    }
}

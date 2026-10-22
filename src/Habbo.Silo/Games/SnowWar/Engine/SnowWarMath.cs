namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>The client's integer maths, which the game must follow exactly: its fast square root and the random
/// number step that seeds each turn's checksum. Divisions elsewhere are C#'s integer ones, which round towards zero
/// as the client's javaDiv does.</summary>
internal static class SnowWarMath
{
    public static int FastSqrt(int value)
    {
        var table = SnowWarTables.SquareRoots;
        if (value >= 65536)
        {
            if (value >= 16777216)
            {
                if (value >= 268435456)
                    return value >= 1073741824 ? table[value >> 24] << 8 : table[value >> 22] << 7;
                return value >= 67108864 ? table[value >> 20] << 6 : table[value >> 18] << 5;
            }

            if (value >= 1048576)
                return value >= 4194304 ? table[value >> 16] << 4 : table[value >> 14] << 3;
            return value >= 262144 ? table[value >> 12] << 2 : table[value >> 10] << 1;
        }

        if (value >= 256)
        {
            if (value >= 4096)
                return value >= 16384 ? table[value >> 8] : table[value >> 6] >> 1;
            return value >= 1024 ? table[value >> 4] >> 2 : table[value >> 2] >> 3;
        }

        return value >= 0 ? table[value] >> 4 : -1;
    }

    /// <summary>The client's xorshift step.</summary>
    public static int IterateSeed(int seed)
    {
        unchecked
        {
            if (seed == 0)
                seed = -1;
            seed ^= seed << 13;
            seed ^= seed >> 17;
            return seed ^ (seed << 5);
        }
    }

    public static int Abs(int value) => value < 0 ? -value : value;
}

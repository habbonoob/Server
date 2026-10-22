using System.Text;

namespace Habbo.Protocol.Rooms;

/// <summary>Parses the classic heightmap string into a <see cref="RoomModel"/>
/// and re-emits the base-36 floor map the client expects in RoomEntryInfo.</summary>
public static class RoomModelParser
{
    private const string FloorMapAlphabet = "0123456789abcdefghijklmnopqrstuvwxyz";

    /// <summary>Parses a heightmap: rows of cells where 'x' is void, '0'-'9' are heights 0-9,
    /// 'a'-'z' are 10-35, 'A'-'Z' are 36-61. The door tile is forced walkable.</summary>
    public static RoomModel Parse(string heightmap, int doorX, int doorY)
    {
        var lines = heightmap.Replace("\r\n", "\r").Replace('\n', '\r')
            .Split('\r', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
            throw new ArgumentException("Heightmap has no rows.", nameof(heightmap));

        var sizeX = lines.Max(line => line.Length);
        var sizeY = lines.Length;
        var heights = new double[sizeX, sizeY];
        var tiles = new bool[sizeX, sizeY];

        for (var y = 0; y < sizeY; y++)
        {
            var line = lines[y];
            for (var x = 0; x < line.Length; x++)
            {
                var cell = line[x];
                if (cell == 'x' || cell == 'X')
                    continue;

                tiles[x, y] = true;
                heights[x, y] = DecodeHeight(cell);
            }
        }

        // The door tile is always walkable even if the map marks it void.
        if (doorX < 0 || doorY < 0 || doorX >= sizeX || doorY >= sizeY)
            throw new ArgumentException($"Door ({doorX},{doorY}) outside {sizeX}x{sizeY} model.");

        tiles[doorX, doorY] = true;

        return new RoomModel(sizeX, sizeY, doorX, doorY, heights, tiles);
    }

    /// <summary>Re-emits the floor map string the client renders: lowercase base-36 heights, 'x' for void.</summary>
    public static string EmitFloorMap(RoomModel model)
    {
        var builder = new StringBuilder(model.SizeX * model.SizeY + model.SizeY);
        for (var y = 0; y < model.SizeY; y++)
        {
            for (var x = 0; x < model.SizeX; x++)
            {
                if (!model.IsTile(x, y))
                {
                    builder.Append('x');
                    continue;
                }

                var height = (int)model.GetHeight(x, y);
                builder.Append(FloorMapAlphabet[Math.Clamp(height, 0, FloorMapAlphabet.Length - 1)]);
            }

            if (y < model.SizeY - 1)
                builder.Append('\r');
        }

        return builder.ToString();
    }

    private static double DecodeHeight(char cell)
    {
        if (cell is >= '0' and <= '9')
            return cell - '0';
        if (cell is >= 'a' and <= 'z')
            return 10 + (cell - 'a');
        if (cell is >= 'A' and <= 'Z')
            return 36 + (cell - 'A');

        throw new ArgumentException($"Unknown heightmap cell '{cell}'.");
    }
}

using System.Globalization;
using System.Text.RegularExpressions;
using Habbo.Protocol.Rooms;

namespace Habbo.Silo.Rooms;

/// <summary>Where a wall item hangs, as the client writes it: ":w=x,y l=x,y r" — the wall tile, the offset on it in
/// pixels, and the side (l or r). Parsed rather than stored as sent, since every visitor's client reads it back.</summary>
public readonly partial record struct WallLocation(int WallX, int WallY, int OffsetX, int OffsetY, char Side)
{
    /// <summary>How far an offset may reach along a wall: well past the tallest wall the floor plan allows.</summary>
    private const int MaxOffset = 2000;

    [GeneratedRegex(@"^:w=(\d{1,3}),(\d{1,3}) l=(-?\d{1,4}),(-?\d{1,4}) ([lr])$")]
    private static partial Regex Pattern();

    /// <summary>The location the text names, if it is well formed and on the room's walls' grid.</summary>
    public static bool TryParse(string text, RoomModel model, out WallLocation location)
    {
        location = default;
        if (Pattern().Match(text.Trim()) is not { Success: true } match)
            return false;

        int Number(int group) => int.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture);
        location = new WallLocation(Number(1), Number(2), Number(3), Number(4), match.Groups[5].Value[0]);
        return location.WallX <= model.SizeX && location.WallY <= model.SizeY
               && Math.Abs(location.OffsetX) <= MaxOffset && Math.Abs(location.OffsetY) <= MaxOffset;
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $":w={WallX},{WallY} l={OffsetX},{OffsetY} {Side}");
}

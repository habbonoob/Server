using System.Globalization;

namespace Habbo.Protocol.Wire;

/// <summary>Number-to-text conversions the client expects. Kept here so generated writers and
/// hand-written code format a value the same way.</summary>
public static class WireFormatting
{
    /// <summary>A height, always with one decimal — the form the client's parsers assume.</summary>
    public static string Height(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);

    /// <summary>An integer sent as text, with no decimal part.</summary>
    public static string Integer(double value) =>
        ((long)value).ToString(CultureInfo.InvariantCulture);

    /// <summary>An integer the client reads with parseInt(value, 16).</summary>
    public static string Hex(double value) => ((long)value).ToString("x", CultureInfo.InvariantCulture);
}

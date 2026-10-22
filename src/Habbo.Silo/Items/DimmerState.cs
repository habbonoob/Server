using System.Globalization;

namespace Habbo.Silo.Items;

/// <summary>A room dimmer's state as the client reads it from the item's legacy string:
/// "onFlag,presetId,effectType,#colour,brightness", where the flag is 2 for on and 1 for off.</summary>
public sealed record DimmerState(bool IsOn, int PresetId, int EffectType, string ColorHex, int Brightness)
{
    private const int Off = 1;
    private const int On = 2;
    private const char Separator = ',';
    private const int Parts = 5;

    public static DimmerState? Parse(string? text)
    {
        var parts = (text ?? string.Empty).Split(Separator);
        if (parts.Length < Parts
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var flag)
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var preset)
            || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var effect)
            || !int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var brightness))
            return null;

        return new DimmerState(flag == On, preset, effect, parts[3], brightness);
    }

    public override string ToString() => string.Join(Separator,
        (IsOn ? On : Off).ToString(CultureInfo.InvariantCulture),
        PresetId.ToString(CultureInfo.InvariantCulture),
        EffectType.ToString(CultureInfo.InvariantCulture),
        ColorHex,
        Brightness.ToString(CultureInfo.InvariantCulture));
}

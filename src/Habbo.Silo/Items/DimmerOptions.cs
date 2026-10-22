namespace Habbo.Silo.Items;

/// <summary>What a room dimmer may be set to (config "Furniture:Dimmer").</summary>
public sealed class DimmerOptions
{
    /// <summary>How many presets a dimmer keeps.</summary>
    public int PresetCount { get; set; } = 3;

    /// <summary>The effects the dimmer offers, by the client's number.</summary>
    public List<int> EffectTypes { get; set; } = [];

    /// <summary>The colours the dimmer's palette offers.</summary>
    public List<string> Colors { get; set; } = [];

    public int MinBrightness { get; set; }

    public int MaxBrightness { get; set; }

    /// <summary>What a preset is before anyone saves it.</summary>
    public int DefaultEffectType { get; set; }

    public string DefaultColor { get; set; } = string.Empty;

    public int DefaultBrightness { get; set; }
}

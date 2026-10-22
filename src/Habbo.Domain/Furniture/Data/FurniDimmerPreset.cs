namespace Habbo.Domain.Furniture.Data;

/// <summary>One of a room dimmer's saved presets.</summary>
public sealed class FurniDimmerPreset
{
    public const int ColorMaxLength = 7;

    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public int PresetId { get; set; }

    /// <summary>The client's dimmer effect: a plain tint, or the background dimmed too.</summary>
    public int EffectType { get; set; }

    public required string ColorHex { get; set; }

    public int Brightness { get; set; }
}

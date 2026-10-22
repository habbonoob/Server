namespace Habbo.Domain.Furniture.Data;

/// <summary>One picked item as a wired box recorded it: its state, where it stood and which way
/// it faced. "Match furni to position and state" puts it back so; its condition checks it is.</summary>
public sealed class FurniWiredSnapshot
{
    /// <summary>As long as an item's own state may be (furni_states).</summary>
    public const int StateMaxLength = 512;

    /// <summary>The wired box the snapshot belongs to.</summary>
    public int WiredItemId { get; set; }

    /// <summary>The item recorded.</summary>
    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public string State { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public double Z { get; set; }
    public int Rotation { get; set; }
}

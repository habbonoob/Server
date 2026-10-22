namespace Habbo.Silo.Groups;

/// <summary>A layer of the badge a new group's editor starts from.</summary>
public sealed class DefaultBadgeLayer
{
    public int PartId { get; set; }

    public int ColorId { get; set; }

    /// <summary>Where on the badge it sits, 0 to 8 across then down; 4 is the middle.</summary>
    public int Position { get; set; }
}

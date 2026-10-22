namespace Habbo.Domain.Groups;

/// <summary>One layer of a group's badge: the base (layer 0) or a symbol on it, in a colour, in
/// one of the nine positions.</summary>
public sealed class GroupBadgeLayer
{
    public int GroupId { get; set; }

    public int Layer { get; set; }

    public int PartId { get; set; }

    public int ColorId { get; set; }

    public int Position { get; set; }
}

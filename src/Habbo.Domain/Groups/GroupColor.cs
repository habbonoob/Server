namespace Habbo.Domain.Groups;

/// <summary>A colour the group editor offers: for badge parts, or as the group's primary or
/// secondary colour. Each list has its own ids.</summary>
public sealed class GroupColor
{
    public int Id { get; set; }

    public GroupColorKind Kind { get; set; }

    /// <summary>Six hex digits, no hash.</summary>
    public string Hex { get; set; } = string.Empty;
}

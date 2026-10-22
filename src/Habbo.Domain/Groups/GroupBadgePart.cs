namespace Habbo.Domain.Groups;

/// <summary>A piece the badge editor offers: a base or a symbol, drawn from its image and, for a
/// two-tone part, a mask image that keeps its own colours. The ids are Habbo's own, which the
/// imager draws badges by.</summary>
public sealed class GroupBadgePart
{
    public int Id { get; set; }

    public GroupBadgePartKind Kind { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string MaskFileName { get; set; } = string.Empty;
}

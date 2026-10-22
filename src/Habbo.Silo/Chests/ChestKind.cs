using System.Globalization;

namespace Habbo.Silo.Chests;

/// <summary>A kind of item as chests tell them apart: wall or floor, the class, and for a poster which one.</summary>
public readonly record struct ChestKind(bool IsWallItem, int ClassId, string PosterId)
{
    /// <summary>How the chest's preview names the kind: "wall,class[,poster]".</summary>
    public string ToPreview() => PosterId.Length > 0
        ? string.Create(CultureInfo.InvariantCulture, $"{(IsWallItem ? "true" : "false")},{ClassId},{PosterId}")
        : string.Create(CultureInfo.InvariantCulture, $"{(IsWallItem ? "true" : "false")},{ClassId}");

    /// <summary>A kind as the chest's preview names it; none for a name it cannot read.</summary>
    public static ChestKind? FromPreview(string preview)
    {
        var parts = preview.Split(',');
        return parts.Length >= 2 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var classId)
            ? new ChestKind(parts[0] == "true", classId, parts.Length > 2 ? parts[2] : string.Empty)
            : null;
    }
}

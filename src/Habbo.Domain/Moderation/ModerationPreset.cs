namespace Habbo.Domain.Moderation;

/// <summary>A ready-made message the mod tool offers, for users or for rooms.</summary>
public sealed class ModerationPreset
{
    public int Id { get; set; }

    public ModerationPresetKind Kind { get; set; }

    public string Text { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

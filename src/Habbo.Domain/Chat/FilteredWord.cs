namespace Habbo.Domain.Chat;

/// <summary>A word the hotel's filter replaces wherever players write: chat, names, mottos.</summary>
public sealed class FilteredWord
{
    public const int WordMaxLength = 64;

    public required string Word { get; set; }

    /// <summary>What it becomes; empty for the hotel's default replacement.</summary>
    public string Replacement { get; set; } = string.Empty;
}

namespace Habbo.Domain.Support;

/// <summary>One reason a player can report something for, inside a
/// <see cref="CallForHelpCategory"/>.</summary>
public sealed class CallForHelpTopic
{
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public CallForHelpCategory? Category { get; set; }

    /// <summary>The localization key the client looks the reason up by.</summary>
    public required string Name { get; set; }

    /// <summary>The client warns the reporter with this before sending — the localization key
    /// for what happens to them if they report dishonestly. Empty for no warning.</summary>
    public string Consequence { get; set; } = string.Empty;

    /// <summary>Where this reason sits within its category.</summary>
    public int SortOrder { get; set; }
}

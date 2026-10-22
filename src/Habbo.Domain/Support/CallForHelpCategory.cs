namespace Habbo.Domain.Support;

/// <summary>A group of report reasons in the client's help tool. The client has no list of its
/// own: what it offers a player to report is whatever CfhTopicsInitMessageComposer carried.</summary>
public sealed class CallForHelpCategory
{
    public int Id { get; set; }

    /// <summary>The localization key the client looks the heading up by.</summary>
    public required string Name { get; set; }

    /// <summary>Where this category sits in the list the client draws.</summary>
    public int SortOrder { get; set; }

    public List<CallForHelpTopic> Topics { get; set; } = [];
}

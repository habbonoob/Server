namespace Habbo.Domain.Navigator;

/// <summary>A saved search shown inside a tab.</summary>
public sealed class NavigatorQuickLink
{
    public int Id { get; set; }

    public required string TabSearchCode { get; set; }
    public NavigatorTab? Tab { get; set; }

    /// <summary>The search code this link runs when clicked.</summary>
    public required string SearchCode { get; set; }

    public string Filter { get; set; } = string.Empty;

    /// <summary>The label the client shows.</summary>
    public required string Localization { get; set; }

    public int SortOrder { get; set; }
}

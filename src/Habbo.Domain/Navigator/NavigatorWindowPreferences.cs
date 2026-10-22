namespace Habbo.Domain.Navigator;

/// <summary>Where a player left the new navigator's window, and how it showed results.</summary>
public sealed class NavigatorWindowPreferences
{
    public Guid UserId { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public bool LeftPaneHidden { get; set; }

    public int ResultsMode { get; set; }
}

namespace Habbo.Silo.Navigator;

/// <summary>Where the new navigator's window opens for a player who never moved it.</summary>
public sealed class NavigatorWindowDefaults
{
    public int X { get; set; } = 68;

    public int Y { get; set; } = 42;

    public int Width { get; set; } = 425;

    public int Height { get; set; } = 592;

    public bool LeftPaneHidden { get; set; }

    public int ResultsMode { get; set; }
}

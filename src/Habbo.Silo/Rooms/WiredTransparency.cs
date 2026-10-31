namespace Habbo.Silo.Rooms;

/// <summary>How wired's set visibility left a furni or user for everyone: how transparent (0 visible, 100 gone), whether
/// clicks pass through it, and the easing it changed with (one of the dialog's five).</summary>
public sealed record WiredTransparency(int Transparency, bool AllowClickThrough, int EasingMode)
{
    public const int Visible = 0;
    public const int Invisible = 100;

    /// <summary>As everything starts out: what need not be kept or told.</summary>
    public bool IsDefault => Transparency == Visible && !AllowClickThrough;
}

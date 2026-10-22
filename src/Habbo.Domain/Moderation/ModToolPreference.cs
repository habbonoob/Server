namespace Habbo.Domain.Moderation;

/// <summary>Where a moderator keeps the mod tool's window.</summary>
public sealed class ModToolPreference
{
    public Guid UserId { get; set; }

    public int WindowX { get; set; }
    public int WindowY { get; set; }
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }
}

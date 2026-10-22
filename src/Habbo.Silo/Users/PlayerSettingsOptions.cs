namespace Habbo.Silo.Users;

/// <summary>A player's own settings (config section "PlayerSettings").</summary>
public sealed class PlayerSettingsOptions
{
    /// <summary>How many words a player's own chat filter holds, and how long each may be.</summary>
    public int MaxFilterWords { get; set; }
    public int MaxFilterWordLength { get; set; }
}

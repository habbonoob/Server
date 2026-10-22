namespace Habbo.Silo.Groups;

/// <summary>The hotel's rules for group forums (config section "GroupForums").</summary>
public sealed class GroupForumOptions
{
    /// <summary>What the compose window asks for at least ("should be at least 10 characters").</summary>
    public int SubjectMinLength { get; set; } = 10;

    public int MessageMinLength { get; set; } = 10;

    /// <summary>How long a player waits between one message and the next.</summary>
    public int PostCooldownSeconds { get; set; } = 30;

    /// <summary>How far back "most active" and the leaderboard count messages.</summary>
    public int ActiveDays { get; set; } = 7;

    /// <summary>The most forums, threads or messages one page returns.</summary>
    public int MaxPageSize { get; set; } = 50;
}

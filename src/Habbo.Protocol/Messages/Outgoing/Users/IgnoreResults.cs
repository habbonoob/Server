namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>What IgnoreResult tells the client happened to its mute list.</summary>
public static class IgnoreResults
{
    public const int Ignored = 1;

    /// <summary>Muted, and the oldest one on the full list let go to make room.</summary>
    public const int IgnoredOldestDropped = 2;

    public const int Unignored = 3;
}

namespace Habbo.Domain.Users;

/// <summary>A player's stock of one avatar effect: how many unused ones they hold, and the one
/// running down, if any. An effect lasts <see cref="Duration"/> seconds from when it is activated;
/// a permanent one never runs out.</summary>
public sealed class UserEffect
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>The effect's number in the effect map.</summary>
    public int EffectId { get; set; }

    public int SubType { get; set; }

    /// <summary>Seconds one of them lasts once activated.</summary>
    public int Duration { get; set; }

    /// <summary>How many are waiting to be activated.</summary>
    public int Inactive { get; set; }

    /// <summary>When the running one was activated; null when none is.</summary>
    public DateTimeOffset? ActivatedAt { get; set; }

    public bool IsPermanent { get; set; }

    /// <summary>When the running one runs out, if one is running and it can.</summary>
    public DateTimeOffset? ExpiresAt =>
        ActivatedAt is { } activated && !IsPermanent ? activated.AddSeconds(Duration) : null;
}

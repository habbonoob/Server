namespace Habbo.Domain.Games;

/// <summary>How one player did in one SnowStorm game, for the leaderboards, the free games and the skill level.</summary>
public sealed class SnowWarGameResult
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public int GameId { get; set; }

    /// <summary>The arena.</summary>
    public int FieldType { get; set; }

    public int Team { get; set; }

    public bool Won { get; set; }

    public int Score { get; set; }

    public int Kills { get; set; }

    public int Deaths { get; set; }

    public int SnowballHits { get; set; }

    public int SnowballHitsTaken { get; set; }

    public int SnowballsThrown { get; set; }

    public int SnowballsCreated { get; set; }

    public int SnowballsFromMachine { get; set; }

    public DateTimeOffset PlayedAt { get; set; } = DateTimeOffset.UtcNow;
}
